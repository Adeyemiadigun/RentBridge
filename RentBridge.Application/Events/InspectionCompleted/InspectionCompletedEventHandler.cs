using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.InspectionCompleted;
using InspectionCompletedEvent = RentBridge.Domain.Aggregates.InspectionCompleted;

public sealed class InspectionCompletedEventHandler(
    ILogger<InspectionCompletedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionCompletedEvent>
{
    public async Task Handle(InspectionCompletedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionCompleted received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var when = notification.ActualDate.ToString("dd/MM/yyyy");
        var subject = "Inspection completed";
        var body = (string firstName) =>
            $"<h3>Inspection completed</h3><p>Hello {firstName},</p>" +
            $"<p>The inspection for lease <strong>{lease.Id}</strong> took place on <strong>{when}</strong>.</p>" +
            "<p>The property now moves forward to the agreement stage.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("InspectionCompleted for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("InspectionCompleted for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}
