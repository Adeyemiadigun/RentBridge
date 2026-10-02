using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.IdentityVerified;
using IdentityVerificationEvent = RentBridge.Domain.Aggregates.IdentityVerified;

public sealed class IdentityVerifiedEventHandler(
    ILogger<IdentityVerifiedEventHandler> logger,
    IUnitOfWork _unitOfWork)
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Deliberately no longer touches any lease. Identity is a user-level
        // fact and escrow release reads User.IdentityVerified at payout time,
        // so there is no per-lease receipt to stamp — and no ordering
        // dependency on whether the tenant verified before or after creating
        // the lease. No escrow-release attempt here either: release is driven
        // solely by the funding-success webhook.

        logger.LogInformation("User identity verified: {UserId}", notification.UserId);
    }
}