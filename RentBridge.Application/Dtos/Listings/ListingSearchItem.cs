using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Listings;

public sealed record ListingSearchItem(
    Guid Id,
    string Title,
    string? Description,
    decimal PriceAmount,
    string Currency,
    ListingStatus Status,
    string? CoverImageKey,
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
    IReadOnlyList<string> Amenities);