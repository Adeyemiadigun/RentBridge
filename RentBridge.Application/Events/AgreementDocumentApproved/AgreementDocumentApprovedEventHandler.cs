using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Events.AgreementDocumentApproved;
using AgreementDocumentApprovedEvent = RentBridge.Domain.Aggregates.AgreementDocumentApproved;

public sealed class AgreementDocumentApprovedEventHandler(
    ILogger<AgreementDocumentApprovedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<AgreementDocumentApprovedEvent>
{
    public async Task Handle(AgreementDocumentApprovedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("AgreementDocumentApproved received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        // Landlord approval opens the tenant's signing step — tell the tenant.
        // Tenant approval also raises AgreementFullySigned, which notifies both
        // parties separately, so this handler only covers the landlord step.
        if (notification.Party != LeaseParty.Landlord)
        {
            return;
        }

        var tenant = await unitOfWork.Repository<User>().GetByIdAsync(lease.TenantUserId, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("AgreementDocumentApproved for lease {LeaseId} has unknown tenant {TenantUserId}", lease.Id, lease.TenantUserId);
            return;
        }

        var subject = "Your tenancy agreement is ready to sign";
        var body =
            $"<h3>Agreement ready for your signature</h3><p>Hello {tenant.FirstName},</p>" +
            $"<p>The landlord's signed copy of the agreement for lease <strong>{lease.Id}</strong> has been " +
            "reviewed and approved. Please download it, sign it and upload the signed copy so the lawyer can complete the review.</p>";

        await emailService.SendEmailAsync(tenant.Email.Value, subject, body, cancellationToken);
    }
}
