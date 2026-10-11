using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.AgreementDocumentUploaded;
using AgreementDocumentUploadedEvent = RentBridge.Domain.Aggregates.AgreementDocumentUploaded;

public sealed class AgreementDocumentUploadedEventHandler(
    ILogger<AgreementDocumentUploadedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<AgreementDocumentUploadedEvent>
{
    public async Task Handle(AgreementDocumentUploadedEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("AgreementDocumentUploaded received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        var subject = "Your tenancy agreement is ready to sign";
        var body = (string firstName) =>
            $"<h3>Agreement ready</h3><p>Hello {firstName},</p>" +
            $"<p>The tenancy agreement for lease <strong>{lease.Id}</strong> has been uploaded by the reviewing lawyer. " +
            "Please download it, sign it and upload the signed copy to continue.</p>";

        // Only the landlord is notified at this step: they sign first, and the
        // tenant is not asked to act until the landlord's signed copy has been
        // reviewed and approved (AgreementDocumentApproved notifies the tenant).
        var landlord = await unitOfWork.Repository<User>().GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is not null)
        {
            await emailService.SendEmailAsync(landlord.Email.Value, subject, body(landlord.FirstName), cancellationToken);
        }
        else
        {
            logger.LogWarning("AgreementDocumentUploaded for lease {LeaseId} has unknown landlord {LandlordUserId}", lease.Id, lease.LandlordUserId);
        }
    }
}
