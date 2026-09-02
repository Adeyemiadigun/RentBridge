using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.IdentityVerified;
using IdentityVerificationEvent = RentBridge.Domain.Aggregates.Users.IdentityVerified;

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

        logger.LogInformation("User identity verified: {UserId}", notification.UserId);
    }
}
