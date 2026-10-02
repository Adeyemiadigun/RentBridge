using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class ConfirmInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowReleaseService releaseService,
    ILogger<ConfirmInspectionCommandHandler> logger)
    : IRequestHandler<ConfirmInspectionCommand, Result<LeaseTransitionResponse>>
{
    private const int MaxAttempts = 3;
    private const int BaseRetryDelayMs = 100;
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);

    public async Task<Result<LeaseTransitionResponse>> Handle(
        ConfirmInspectionCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<LeaseTransitionResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        // The lease uses an xmin rowversion, so a concurrent writer surfaces as a
        // DbUpdateConcurrencyException. Load + mutate + save run under a bounded
        // retry; the release attempt is deliberately OUTSIDE that retry so a
        // failure there can never re-enter a confirmation that already committed.
        Guid? leaseId = null;
        string? newStatus = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var lease = await unitOfWork.Leases
                    .GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
                if (lease is null)
                {
                    logger.LogInformation("Lease {leaseId} not found", request.LeaseId);
                    return Result<LeaseTransitionResponse>.Fail("Lease not found");
                }

                if (lease.LandlordUserId != user.Id && user.Role != UserRole.Admin)
                {
                    logger.LogInformation(
                        "User {userId} is not authorized to confirm inspection for lease {leaseId}",
                        user.Id, request.LeaseId);
                    return Result<LeaseTransitionResponse>.Forbid("Only the landlord or an admin can confirm an inspection");
                }

                var result = lease.ConfirmInspection(request.ScheduledDate, request.Notes);
                if (!result.IsSuccess)
                {
                    logger.LogInformation(
                        "Lease {leaseId} cannot confirm inspection: {error}", request.LeaseId, result.Error);
                    return Result<LeaseTransitionResponse>.Fail(result.Error!);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                leaseId = lease.Id;
                newStatus = lease.Status.ToString();
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    "Concurrency conflict confirming inspection for lease {leaseId}, attempt {attempt}/{max}",
                    request.LeaseId, attempt, MaxAttempts);
                unitOfWork.ClearChangeTracker();
                await Task.Delay(TimeSpan.FromMilliseconds(BaseRetryDelayMs * attempt), cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex,
                    "Concurrency conflict confirming inspection for lease {leaseId} after {max} attempts",
                    request.LeaseId, MaxAttempts);
                return Result<LeaseTransitionResponse>.Fail("The lease was modified by another process. Please try again.");
            }
        }

        if (leaseId is null || newStatus is null)
        {
            return Result<LeaseTransitionResponse>.Fail("The lease was modified by another process. Please try again.");
        }

        // Inspection is one of the three release gates; if escrow is already
        // funded and this was the last gate, the payout runs now.
        // Run with a short timeout so a slow payment provider never blocks the confirmation response.
        // If the auto-release fails or times out, the reconciliation job will retry it later.
        _ = Task.Run(async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ReleaseTimeout);
            try
            {
                await releaseService.TryAutoReleaseAsync(leaseId.Value, cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Auto-release timed out after {Timeout}s for lease {LeaseId}; will be retried by reconciliation job",
                    ReleaseTimeout.TotalSeconds, leaseId.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-release failed for lease {LeaseId}; will be retried by reconciliation job", leaseId.Value);
            }
        });

        return Result<LeaseTransitionResponse>.Ok(
            new LeaseTransitionResponse(leaseId.Value, newStatus));
    }
}
