namespace RentBridge.Application.Dtos.Lease;

/// <summary>
/// A single lease row returned by GET /leases for the caller. Includes
/// parties (names resolved at query time) and the latest inspection
/// request state so landlord/tenant dashboards can render without
/// fetching each lease individually.
/// </summary>
public sealed record LeaseListItem(
    Guid LeaseId,
    Guid ListingId,
    string? ListingTitle,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    LeasePartyItem? Tenant,
    LeasePartyItem? Landlord,
    LeasePartyItem? Lawyer,
    InspectionListItem? Inspection);

public sealed record LeasePartyItem(Guid Id, string Name, string? Email);

public sealed record InspectionListItem(
    Guid Id,
    string Status,
    DateTimeOffset? PreferredDate,
    DateTimeOffset? ScheduledDate,
    string? Note);