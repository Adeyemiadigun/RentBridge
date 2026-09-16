using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Events.ListingPublished;
using ListingPublishedEvent = RentBridge.Domain.Aggregates.ListingPublished;

public sealed class ListingPublishedEventHandler(
    ILogger<ListingPublishedEventHandler> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService)
    : INotificationHandler<ListingPublishedEvent>
{
    public async Task Handle(ListingPublishedEvent notification, CancellationToken cancellationToken)
    {
        var listing = await unitOfWork.Repository<ListingAggregate>().GetByIdAsync(notification.ListingId, cancellationToken);
        if (listing is null)
        {
            logger.LogWarning("ListingPublished received for unknown listing {ListingId}", notification.ListingId);
            return;
        }

        var owner = await unitOfWork.Repository<User>().GetByIdAsync(listing.OwnerUserId, cancellationToken);
        if (owner is null)
        {
            logger.LogWarning("ListingPublished for listing {ListingId} has unknown owner {OwnerUserId}", listing.Id, listing.OwnerUserId);
            return;
        }

        var subject = "Your listing is now live";
        var body = $"<h3>Listing published</h3><p>Hello {owner.FirstName},</p>" +
                   $"<p>Your listing <strong>{listing.Title}</strong> is now live and visible to tenants.</p>" +
                   $"<p>Listing ID: <strong>{listing.Id}</strong></p>";

        await emailService.SendEmailAsync(owner.Email.Value, subject, body, cancellationToken);
    }
}