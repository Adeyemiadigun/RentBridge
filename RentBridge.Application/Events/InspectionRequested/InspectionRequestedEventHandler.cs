using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.InspectionRequested;
using InspectionRequestedEvent = RentBridge.Domain.Aggregates.Users.InspectionRequested;

public sealed class InspectionRequestedEventHandler(
    ILogger<InspectionRequestedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionRequestedEvent>
{
    public async Task Handle(InspectionRequestedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionRequested received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            logger.LogWarning("InspectionRequested for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
            return;
        }

        var subject = "An inspection has been requested";
        var body = $"<h3>Inspection requested</h3><p>Hello {landlord.FirstName},</p>" +
                   $"<p>The tenant has requested an inspection for your property.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>" +
                   $"<p>Preferred date: <strong>{notification.PreferredDate:dd/MM/yyyy HH:mm}</strong></p>" +
                   $"<p>Please log in to confirm or decline the request.</p>";

        await emailService.SendEmailAsync(landlord.Email.Value, subject, body, cancellationToken);
    }
}