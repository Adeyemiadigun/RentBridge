using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
namespace RentBridge.Application.Command.Lease;

public sealed class BeginLegalReviewCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    IAgreementDocumentService agreementService,
    ILogger<BeginLegalReviewCommandHandler> logger)
    : IRequestHandler<BeginLegalReviewCommand, Result>
{
    private const int MaxAttempts = 3;
    private const int BaseRetryDelayMs = 100;

    public async Task<Result> Handle(BeginLegalReviewCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                // Must use the dedicated loader: FindAsync resolves the xmin
                // rowversion correctly and eagerly loads InspectionRequests.
                // A plain LINQ FirstOrDefault tracks the lease without the
                // owned-entity graph, so the save that follows updates 0 rows
                // and throws. EnsureComposedAsync also reads that collection
                // to stamp the inspection date into the agreement terms.
                var lease = await unitOfWork.Leases
                    .GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
                if (lease is null)
                {
                    logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
                    return Result.Fail("Lease not found");
                }

                var isParty = user.Id == lease.LandlordUserId || user.Id == lease.TenantUserId;
                var isStaff = user.Role is UserRole.Lawyer or UserRole.Admin;
                if (!isParty && !isStaff)
                {
                    logger.LogInformation("User {UserId} is not a party to lease {LeaseId}", user.Id, request.LeaseId);
                    return Result.Forbid("Only the tenant, landlord, lawyer, or admin can begin legal review.");
                }

                // Assign a lawyer BEFORE composing: BuildTerms reads AssignedLawyerId,
                // so composing first drafted an agreement naming no lawyer at all.
                if (lease.AssignedLawyerId is null)
                {
                    var picked = await lawyerService.PickNextVerifiedLawyerAsync(cancellationToken);
                    if (!picked.IsSuccess)
                    {
                        logger.LogWarning("No verified lawyer available for lease {LeaseId}: {Error}", request.LeaseId, picked.Error);
                        return Result.Fail("No verified lawyer is currently available. Contact an admin.");
                    }

                    var assign = lease.AssignLawyer(picked.Value);
                    if (!assign.IsSuccess)
                    {
                        logger.LogInformation(
                            "Lease {LeaseId} cannot be assigned lawyer {LawyerId}: {Error}",
                            request.LeaseId, picked.Value, assign.Error);
                        return Result.Fail(assign.Error!);
                    }
                }

                var composed = await agreementService.EnsureComposedAsync(lease, cancellationToken);
                if (!composed.IsSuccess)
                {
                    logger.LogWarning("Cannot compose agreement for lease {LeaseId}: {Error}", request.LeaseId, composed.Error);
                    return Result.Fail(composed.Error!);
                }

                var begin = lease.BeginLegalReview();
                if (!begin.IsSuccess)
                {
                    logger.LogInformation("Lease {LeaseId} cannot begin legal review: {Error}", request.LeaseId, begin.Error);
                    return Result.Fail(begin.Error!);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    "Concurrency conflict beginning legal review for lease {LeaseId}, attempt {attempt}/{max}",
                    request.LeaseId, attempt, MaxAttempts);
                unitOfWork.ClearChangeTracker();
                await Task.Delay(TimeSpan.FromMilliseconds(BaseRetryDelayMs * attempt), cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Name the entity/table that matched 0 rows. Without this the
                // message is indistinguishable from a genuine lost-update race.
                foreach (var entry in ex.Entries)
                {
                    var key = entry.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")
                        .ToArray();
                    logger.LogError(
                        ex,
                        "Concurrency conflict on entity {Entity} [{Keys}] for lease {LeaseId} after {max} attempts. State: {State}",
                        entry.Metadata.ClrType.Name,
                        string.Join(", ", key),
                        request.LeaseId,
                        MaxAttempts,
                        entry.State);
                }

                return Result.Fail("The lease was modified by another process. Please try again.");
            }
        }

        return Result.Fail("The lease was modified by another process. Please try again.");
    }
}