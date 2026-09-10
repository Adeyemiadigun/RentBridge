using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.EscrowFunded;
using EscrowFundedEvent = RentBridge.Domain.Aggregates.Users.EscrowFunded;

public sealed class EscrowFundedEventHandler(
    ILogger<EscrowFundedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<EscrowFundedEvent>
{
    public async Task Handle(EscrowFundedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("EscrowFunded received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Rent deposited into escrow";
        var body = (string firstName) =>
            $"<h3>Escrow funded</h3><p>Hello {firstName},</p>" +
            $"<p>Rent for lease <strong>{lease.Id}</strong> has been deposited into escrow and is protected until verification, inspection, and legal review are complete.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("EscrowFunded for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("EscrowFunded for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}