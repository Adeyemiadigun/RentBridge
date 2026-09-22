using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.InspectionConfirmed;
using InspectionConfirmedEvent = RentBridge.Domain.Aggregates.InspectionConfirmed;

public sealed class InspectionConfirmedEventHandler(
    ILogger<InspectionConfirmedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<InspectionConfirmedEvent>
{
    public async Task Handle(InspectionConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("InspectionConfirmed received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Inspection confirmed";
        var body = (string firstName) =>
            $"<h3>Inspection confirmed</h3><p>Hello {firstName},</p>" +
            $"<p>The inspection for lease <strong>{lease.Id}</strong> has been confirmed.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("InspectionConfirmed for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("InspectionConfirmed for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}