using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.InspectionRescheduled;
using InspectionRescheduledEvent = RentBridge.Domain.Aggregates.Users.InspectionRescheduled;

public sealed class InspectionRescheduledEventHandler(
    ILogger<InspectionRescheduledEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionRescheduledEvent>
{
    public async Task Handle(InspectionRescheduledEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionRescheduled received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("InspectionRescheduled for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
            return;
        }

        var subject = "Your inspection has been rescheduled";
        var body = $"<h3>Inspection rescheduled</h3><p>Hello {tenant.FirstName},</p>" +
                   $"<p>Your inspection request has been <strong>rescheduled</strong>.</p>" +
                   $"<p>Lease ID: <strong>{lease.Id}</strong></p>" +
                   $"<p>New inspection date: <strong>{notification.NewDate:dd/MM/yyyy HH:mm}</strong></p>";

        await emailService.SendEmailAsync(tenant.Email.Value, subject, body, cancellationToken);
    }
}