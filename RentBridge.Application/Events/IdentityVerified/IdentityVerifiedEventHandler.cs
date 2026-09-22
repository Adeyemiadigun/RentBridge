using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Events.IdentityVerified;
using IdentityVerificationEvent = RentBridge.Domain.Aggregates.IdentityVerified;

public sealed class IdentityVerifiedEventHandler(
    ILogger<IdentityVerifiedEventHandler> logger,
    IUnitOfWork _unitOfWork,
    IEscrowReleaseService _releaseService)
    : INotificationHandler<IdentityVerificationEvent>
{
    public async Task Handle(IdentityVerificationEvent notification, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("IdentityVerified received for unknown user {UserId}", notification.UserId);
            return;
        }

        user.MarkIdentityVerified(notification.KycVerificationId);

        // The identity gate on every lease this user tenants is stamped from the
        // automated verification result — escrow release depends on it.
        var leases = await _unitOfWork.Repository<Lease>()
            .FindAsync(l => l.TenantUserId == notification.UserId, cancellationToken);

        var stamped = 0;
        foreach (var lease in leases)
        {
            if (lease.Status == LeaseStatus.Cancelled) continue;

            var gate = lease.RecordIdentityGate();
            if (gate.IsSuccess)
            {
                stamped++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Identity is one of the three release gates; if escrow is already
        // funded and this was the last gate, the payout runs now.
        foreach (var lease in leases)
        {
            if (lease.Status == LeaseStatus.Cancelled) continue;
            await _releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);
        }

        logger.LogInformation(
            "User identity verified: {UserId}; identity gate stamped on {LeaseCount} lease(s)",
            notification.UserId, stamped);
    }
}