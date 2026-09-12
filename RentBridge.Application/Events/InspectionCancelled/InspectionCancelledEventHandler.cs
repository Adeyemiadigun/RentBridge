using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.InspectionCancelled;
using InspectionCancelledEvent = RentBridge.Domain.Aggregates.Users.InspectionCancelled;

public sealed class InspectionCancelledEventHandler(
    ILogger<InspectionCancelledEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionCancelledEvent>
{
    public async Task Handle(InspectionCancelledEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionCancelled received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            logger.LogWarning("InspectionCancelled for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
            return;
        }

        var subject = "An inspection request was cancelled";
        var body = $"<h3>Inspection cancelled</h3><p>Hello {landlord.FirstName},</p>" +
                   $"<p>The tenant has cancelled their inspection request for this property.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>";

        await emailService.SendEmailAsync(landlord.Email.Value, subject, body, cancellationToken);
    }
}