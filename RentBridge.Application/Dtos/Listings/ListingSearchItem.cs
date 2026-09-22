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
    string Street,
    string City,
    string Area,
    string State);