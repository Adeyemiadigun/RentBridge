using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.InspectionRescheduleRejected;
using InspectionRescheduleRejectedEvent = RentBridge.Domain.Aggregates.InspectionRescheduleRejected;

public sealed class InspectionRescheduleRejectedEventHandler(
    ILogger<InspectionRescheduleRejectedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionRescheduleRejectedEvent>
{
    public async Task Handle(InspectionRescheduleRejectedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionRescheduleRejected received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("InspectionRescheduleRejected for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
            return;
        }

        var subject = "Your reschedule request was rejected";
        var body = $"<h3>Reschedule rejected</h3><p>Hello {tenant.FirstName},</p>" +
                   $"<p>Your request to reschedule the inspection was <strong>rejected</strong>.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>" +
                   $"<p>The previously agreed inspection date still stands.</p>";

        await emailService.SendEmailAsync(tenant.Email.Value, subject, body, cancellationToken);
    }
}