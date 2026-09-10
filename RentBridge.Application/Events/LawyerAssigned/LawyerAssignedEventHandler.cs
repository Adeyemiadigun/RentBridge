using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Application.Events.LawyerAssigned;
using LawyerAssignedEvent = RentBridge.Domain.Aggregates.Users.LawyerAssigned;

public sealed class LawyerAssignedEventHandler(
    ILogger<LawyerAssignedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<LawyerAssignedEvent>
{
    public async Task Handle(LawyerAssignedEvent notification, CancellationToken cancellationToken)
    {
        var lawyer = await unitOfWork.Repository<User>().GetByIdAsync(notification.LawyerId, cancellationToken);
        if (lawyer is null)
        {
            logger.LogWarning("LawyerAssigned received for unknown lawyer {LawyerId}", notification.LawyerId);
            return;
        }

        var subject = "You have been assigned to a lease";
        var body = $"<h3>Lawyer assignment</h3><p>Hello {lawyer.FirstName},</p>" +
                   $"<p>You have been assigned to review a tenancy agreement.</p>" +
                   $"<p>Lease ID: <strong>{notification.LeaseId}</strong></p>" +
                   $"<p>Please log in to review and certify the agreement.</p>";

        await emailService.SendEmailAsync(lawyer.Email.Value, subject, body, cancellationToken);
    }
}