using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.AgreementCertified;
using AgreementCertifiedEvent = RentBridge.Domain.Aggregates.Users.AgreementCertified;

public sealed class AgreementCertifiedEventHandler(
    ILogger<AgreementCertifiedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<AgreementCertifiedEvent>
{
    public async Task Handle(AgreementCertifiedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("AgreementCertified received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Your agreement has been certified";
        var body = (string firstName) =>
            $"<h3>Agreement certified</h3><p>Hello {firstName},</p>" +
            $"<p>The tenancy agreement for lease <strong>{lease.Id}</strong> has been reviewed and certified by a lawyer and is ready for your signature.</p>";

        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("AgreementCertified for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is not null)
        {
            await emailService.SendEmailAsync(tenant.Email.Value, subject, body(tenant.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("AgreementCertified for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
        }
    }
}