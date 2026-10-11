using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Events.AgreementDocumentSubmittedForReview;
using AgreementDocumentSubmittedForReviewEvent = RentBridge.Domain.Aggregates.AgreementDocumentSubmittedForReview;

public sealed class AgreementDocumentSubmittedForReviewEventHandler(
    ILogger<AgreementDocumentSubmittedForReviewEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<AgreementDocumentSubmittedForReviewEvent>
{
    public async Task Handle(AgreementDocumentSubmittedForReviewEvent notification, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(notification.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("AgreementDocumentSubmittedForReview received for unknown lease {LeaseId}", notification.LeaseId);
            return;
        }

        if (lease.AssignedLawyerId is not Guid lawyerId)
        {
            logger.LogWarning("AgreementDocumentSubmittedForReview for lease {LeaseId} has no assigned lawyer", lease.Id);
            return;
        }

        var lawyer = await unitOfWork.Repository<User>().GetByIdAsync(lawyerId, cancellationToken);
        if (lawyer is null)
        {
            logger.LogWarning("AgreementDocumentSubmittedForReview for lease {LeaseId} has unknown lawyer {LawyerId}", lease.Id, lawyerId);
            return;
        }

        var subject = "A signed agreement is awaiting your review";
        var body =
            $"<h3>Agreement awaiting review</h3><p>Hello {lawyer.FirstName},</p>" +
            $"<p>The {notification.Party} has submitted their signed copy of the agreement for lease " +
            $"<strong>{lease.Id}</strong>. Please review and approve it.</p>";

        await emailService.SendEmailAsync(lawyer.Email.Value, subject, body, cancellationToken);
    }
}
