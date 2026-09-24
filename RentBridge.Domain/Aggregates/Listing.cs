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
    public ListingType ListingType { get; private set; } = ListingType.Rent;
    public PaymentPlan PaymentPlan { get; private set; } = PaymentPlan.Outright;
    public Money? CautionFee { get; private set; }
    public string? OtherExpenses { get; private set; }
    public Money? RealHouseFee { get; private set; }
    public Money? AgentFee { get; private set; }
    public ListingStatus Status { get; private set; }
    public string? CoverImageKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private Listing() { }

    public Listing(
        Guid ownerUserId,
        Guid propertyId,
        string title,
        Money price,
        string? description = null,
        ListingType listingType = ListingType.Rent,
        PaymentPlan paymentPlan = PaymentPlan.Outright,
        Money? cautionFee = null,
        string? otherExpenses = null,
        Money? realHouseFee = null,
        Money? agentFee = null) : this()
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        PropertyId = propertyId;
        Title = title;
        Price = price;
        Description = description;
        ListingType = listingType;
        PaymentPlan = paymentPlan;
        CautionFee = cautionFee;
        OtherExpenses = otherExpenses;
        RealHouseFee = realHouseFee;
        AgentFee = agentFee;
        Status = ListingStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Result Publish()
    {
        if (Status is not (ListingStatus.Draft or ListingStatus.Unpublished))
            return Result.Fail("Only drafts or unpublished listings can be published");
        Status = ListingStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        Raise(new ListingPublished(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Updates mutable listing details. Closed listings are terminal and
    /// cannot be edited. Null arguments leave the current value unchanged.
    /// </summary>
    public Result UpdateDetails(string? title, string? description, Money? price)
    {
        if (Status == ListingStatus.Closed) return Result.Fail("Closed listings cannot be edited");
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (price is not null) Price = price;
        return Result.Ok();
    }

    public Result Unpublish()
    {
        if (Status != ListingStatus.Published) return Result.Fail("Only published listings can be unpublished");
        Status = ListingStatus.Unpublished;
        return Result.Ok();
    }

    public Result Close()
    {
        if (Status == ListingStatus.Closed) return Result.Fail("Listing is already closed");
        Status = ListingStatus.Closed;
        return Result.Ok();
    }
}

public record ListingPublished(Guid ListingId) : IDomainEvent;
