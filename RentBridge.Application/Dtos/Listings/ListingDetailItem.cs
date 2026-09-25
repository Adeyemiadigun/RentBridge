using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Listings;

/// <summary>
/// A single published listing with its property and owner details,
/// returned by GET /listings/{listingId}.
/// </summary>
public sealed record ListingDetailItem(
    Guid Id,
    string Title,
    string? Description,
    decimal PriceAmount,
    string Currency,
    ListingStatus Status,
    string? CoverImageKey,
    IReadOnlyList<string> ImageUrls,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    Guid PropertyId,
    ListingType ListingType,
    PaymentPlan PaymentPlan,
    decimal? CautionFeeAmount,
    decimal? RealHouseFeeAmount,
    decimal? AgentFeeAmount,
    string Street,
    string City,
    string Area,
    string State,
    string? PropertyType,
    int Bedrooms,
    int Bathrooms,
    string? AvailableFrom,
    IReadOnlyList<string> Amenities,
    Guid OwnerUserId,
    string OwnerName,
    string? OwnerEmail,
    bool OwnerVerified);