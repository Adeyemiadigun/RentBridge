using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.InspectionRescheduleRequestedEvent;


public sealed class InspectionRescheduleRequestedEventHandler(
    ILogger<InspectionRescheduleRequestedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionRescheduleRequested>
{
    public async Task Handle(InspectionRescheduleRequested notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionRescheduleRequested received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            logger.LogWarning("InspectionRescheduleRequested for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
            return;
        }

        var subject = "Inspection reschedule requested";
        var body = $"<h3>Reschedule request</h3><p>Hello {landlord.FirstName},</p>" +
                   $"<p>The tenant has requested to reschedule the inspection for your property.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>" +
                   $"<p>Proposed date: <strong>{notification.ProposedDate:dd/MM/yyyy HH:mm}</strong></p>" +
                   $"<p>Please log in to confirm or reject the new date.</p>";

        await emailService.SendEmailAsync(landlord.Email.Value, subject, body, cancellationToken);
    }
}