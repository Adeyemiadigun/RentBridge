using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.LeaseCreated;
using LeaseCreatedEvent = RentBridge.Domain.Aggregates.Users.LeaseCreated;

public sealed class LeaseCreatedEventHandler(
    ILogger<LeaseCreatedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<LeaseCreatedEvent>
{
    public async Task Handle(LeaseCreatedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("LeaseCreated received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            logger.LogWarning("LeaseCreated for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
            return;
        }

        var subject = "A tenant is interested in your property";
        var body = $"<h3>New lease request</h3><p>Hello {landlord.FirstName},</p>" +
                   $"<p>A tenant has shown interest in your property and initiated a lease.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>" +
                   $"<p>Please log in to begin the inspection flow.</p>";

        await emailService.SendEmailAsync(landlord.Email.Value, subject, body, cancellationToken);
    }
}