using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

public class Listing : Entity<Guid>
{
    public Guid OwnerUserId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public ListingStatus Status { get; private set; }
    public string? CoverImageKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private Listing() { }

    public Listing(Guid ownerUserId, Guid propertyId, string title, Money price, string? description = null)
        : this()
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        PropertyId = propertyId;
        Title = title;
        Price = price;
        Description = description;
        Status = ListingStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Result MarkPendingVerification()
    {
        if (Status != ListingStatus.Draft) return Result.Fail("Only drafts can enter verification");
        Status = ListingStatus.PendingVerification;
        Raise(new ListingPendingVerification(Id));
        return Result.Ok();
    }

    public Result Publish()
    {
        if (Status != ListingStatus.PendingVerification) return Result.Fail("Must pass verification first");
        Status = ListingStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        Raise(new ListingPublished(Id));
        return Result.Ok();
    }
}

public record ListingPendingVerification(Guid ListingId) : IDomainEvent;
public record ListingPublished(Guid ListingId) : IDomainEvent;
