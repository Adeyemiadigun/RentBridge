using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Escrow payout to the landlord is automatic. This command is the admin-only
/// fallback used to retry a failed/stuck payout; it delegates to the same
/// release service so the gates and idempotency rules are identical.
/// </summary>
public sealed class ReleaseEscrowCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowReleaseService releaseService,
    ILogger<ReleaseEscrowCommandHandler> logger)
    : IRequestHandler<ReleaseEscrowCommand, Result>
{
    public async Task<Result> Handle(ReleaseEscrowCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role is not UserRole.Admin)
        {
            logger.LogInformation("Non-admin {UserId} attempted an escrow payout override", actor.Id);
            return Result.Fail("Escrow payouts are automatic. Only an admin can retry or force a payout.");
        }

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found.");
        }

        if (lease.Status == LeaseStatus.Released)
        {
            return Result.Ok();
        }

        // Give an admin-driven retry a fresh attempt budget.
        var payment = lease.EscrowPayments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status is EscrowStatus.Funded
                or EscrowStatus.Releasing
                or EscrowStatus.PayoutFailed);
        payment?.ResetPayoutAttempts();

        var result = await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);

        // A privileged payout action must leave a trail whether or not it worked.
        unitOfWork.Repository<AuditLog>().Add(
            new AuditLog(
                actor.Id,
                result.IsSuccess ? "EscrowPayoutRetriedByAdmin" : "EscrowPayoutRetryFailedByAdmin",
                targetType: "Lease",
                targetId: lease.Id,
                details: result.IsSuccess
                    ? $"{{\"paymentReference\":\"{payment?.Reference}\"}}"
                    : $"{{\"paymentReference\":\"{payment?.Reference}\",\"error\":\"{result.Error}\"}}"));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Admin payout retry failed for lease {LeaseId}: {Error}", lease.Id, result.Error);
        }

        return result;
    }
}
