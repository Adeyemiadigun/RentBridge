using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Events.OwnershipVerified;
using OwnershipVerifiedEvent = RentBridge.Domain.Aggregates.Property.OwnershipVerified;

public sealed class OwnershipVerifiedEventHandler(
    ILogger<OwnershipVerifiedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<OwnershipVerifiedEvent>
{
    public async Task Handle(OwnershipVerifiedEvent notification, CancellationToken cancellationToken)
    {
        var property = await unitOfWork.Repository<PropertyAggregate>().GetByIdAsync(notification.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogWarning("OwnershipVerified received for unknown property {PropertyId}", notification.PropertyId);
            return;
        }

        var owner = await unitOfWork.Repository<User>().GetByIdAsync(property.OwnerUserId, cancellationToken);
        if (owner is null)
        {
            logger.LogWarning("OwnershipVerified for property {PropertyId} has unknown owner {OwnerUserId}", property.Id, property.OwnerUserId);
            return;
        }

        var subject = "Your property has been verified";
        var body = $"<h3>Property verified</h3><p>Hello {owner.FirstName},</p>" +
                   $"<p>Your property has been <strong>verified</strong> by our legal team.</p>" +
                   $"<p>Property ID: <strong>{property.Id}</strong></p>" +
                   $"<p>You can now create a listing for this property.</p>";

        await emailService.SendEmailAsync(owner.Email.Value, subject, body, cancellationToken);
    }
}