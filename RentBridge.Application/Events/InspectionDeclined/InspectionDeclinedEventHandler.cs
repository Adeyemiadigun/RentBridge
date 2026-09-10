using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.InspectionDeclined;
using InspectionDeclinedEvent = RentBridge.Domain.Aggregates.Users.InspectionDeclined;

public sealed class InspectionDeclinedEventHandler(
    ILogger<InspectionDeclinedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionDeclinedEvent>
{
    public async Task Handle(InspectionDeclinedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionDeclined received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("InspectionDeclined for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
            return;
        }

        var subject = "Inspection declined";
        var body = $"<h3>Inspection declined</h3><p>Hello {tenant.FirstName},</p>" +
                   $"<p>Your inspection request for lease <strong>{lease.Id}</strong> has been declined.</p>" +
                   $"<p>You can request a new inspection date if you wish.</p>";

        await emailService.SendEmailAsync(tenant.Email.Value, subject, body, cancellationToken);
    }
}