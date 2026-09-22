using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Admin;

/// <summary>One row of the admin lawyer panel (verify/suspend/reject queue).</summary>
public sealed record LawyerPanelItem(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? BarNumber,
    LawyerStatus? Status,
    bool IdentityVerified,
    DateTimeOffset? LastAssignedAt);

/// <summary>One row of the admin listing moderation queue.</summary>
public sealed record ListingModerationItem(
    Guid ListingId,
    string Title,
    decimal PriceAmount,
    string Currency,
    ListingStatus Status,
    Guid OwnerUserId,
    Guid PropertyId,
    bool PropertyVerified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
