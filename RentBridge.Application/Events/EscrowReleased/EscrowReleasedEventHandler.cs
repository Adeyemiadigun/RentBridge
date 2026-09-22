using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.EscrowReleased;
using EscrowReleasedEvent = RentBridge.Domain.Aggregates.EscrowReleased;

public sealed class EscrowReleasedEventHandler(
    ILogger<EscrowReleasedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<EscrowReleasedEvent>
{
    public async Task Handle(EscrowReleasedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("EscrowReleased received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Escrow released";
        var body = (string firstName) =>
            $"<h3>Escrow released</h3><p>Hello {firstName},</p>" +
            $"<p>Escrow for lease <strong>{lease.Id}</strong> has been released to the landlord.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("EscrowReleased for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("EscrowReleased for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}