using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.AgreementFullySigned;
using AgreementFullySignedEvent = RentBridge.Domain.Aggregates.AgreementFullySigned;

public sealed class AgreementFullySignedEventHandler(
    ILogger<AgreementFullySignedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<AgreementFullySignedEvent>
{
    public async Task Handle(AgreementFullySignedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("AgreementFullySigned received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Agreement fully signed";
        var body = (string firstName) =>
            $"<h3>Agreement signed</h3><p>Hello {firstName},</p>" +
            $"<p>The tenancy agreement for lease <strong>{lease.Id}</strong> has been signed by all parties.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("AgreementFullySigned for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("AgreementFullySigned for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}