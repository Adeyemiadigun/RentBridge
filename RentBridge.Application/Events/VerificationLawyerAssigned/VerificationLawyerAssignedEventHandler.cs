using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Events.VerificationLawyerAssigned;
using VerificationLawyerAssignedEvent = RentBridge.Domain.Aggregates.VerificationLawyerAssigned;

public sealed class VerificationLawyerAssignedEventHandler(
    ILogger<VerificationLawyerAssignedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<VerificationLawyerAssignedEvent>
{
    public async Task Handle(VerificationLawyerAssignedEvent notification, CancellationToken cancellationToken)
    {
        var property = await unitOfWork.Repository<PropertyAggregate>().GetByIdAsync(notification.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogWarning("VerificationLawyerAssigned received for unknown property {PropertyId}", notification.PropertyId);
            return;
        }

        var lawyer = await unitOfWork.Repository<User>().GetByIdAsync(notification.LawyerId, cancellationToken);
        if (lawyer is null)
        {
            logger.LogWarning("VerificationLawyerAssigned for property {PropertyId} has unknown lawyer {LawyerId}", property.Id, notification.LawyerId);
            return;
        }

        if (notification.PreviousLawyerId is not null)
        {
            logger.LogInformation(
                "Verification lawyer for property {PropertyId} reassigned from {PreviousLawyerId} to {LawyerId}",
                property.Id, notification.PreviousLawyerId, notification.LawyerId);
        }

        var subject = "A property has been assigned to you for verification";
        var body = $"<h3>Property verification assignment</h3><p>Hello {lawyer.FirstName},</p>" +
                   $"<p>You have been assigned to verify ownership of a property.</p>" +
                   $"<p>Property ID: <strong>{property.Id}</strong></p>" +
                   $"<p>Please log in to review the ownership documents.</p>";

        await emailService.SendEmailAsync(lawyer.Email.Value, subject, body, cancellationToken);
    }
}