using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Events.EscrowReleased;
using EscrowReleasedEvent = RentBridge.Domain.Aggregates.EscrowReleased;

/// <summary>
/// Writes the released split to the ledger (commission, legal fee, net payout).
/// Runs off the lease's <see cref="EscrowReleasedEvent"/>, which is raised once
/// when <see cref="Lease.Release"/> transitions the lease, so these lines are
/// produced exactly once regardless of which path finalized the payout.
/// </summary>
public sealed class EscrowReleasedLedgerHandler(
    IUnitOfWork unitOfWork,
    ILedgerService ledgerService,
    ILogger<EscrowReleasedLedgerHandler> logger)
    : INotificationHandler<EscrowReleasedEvent>
{
    public async Task Handle(EscrowReleasedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("EscrowReleased ledger write skipped: lease {LeaseId} not found", notification.LeaseId);
            return;
        }

        var payment = lease.EscrowPayments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status == EscrowStatus.Released)
            ?? lease.EscrowPayments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

        if (payment is null)
        {
            logger.LogWarning("EscrowReleased ledger write skipped: no payment on lease {LeaseId}", notification.LeaseId);
            return;
        }

        await ledgerService.RecordReleaseAsync(lease, payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
