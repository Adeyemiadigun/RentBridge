using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Lease;

/// <summary>
/// A single lease row returned by GET /leases for the caller. Includes
/// parties (names resolved at query time) and the latest inspection
/// request state so landlord/tenant dashboards can render without
/// fetching each lease individually. SignedParties lets the dashboard hide
/// the sign action for a party that has already signed.
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
    InspectionListItem? Inspection,
    IReadOnlyList<LeaseParty> SignedParties,
    AgreementProgressItem? Agreement = null,
    LeaseEscrowSummary? Escrow = null);

/// <summary>
/// The latest escrow payment on the lease, flattened for dashboards so a
/// payments view can render one row per lease (tenant + amount + status)
/// without a second request or a ledger that grows a line per payout retry.
/// Amount is the gross the tenant paid; PayoutAmount is what the landlord
/// receives after fees.
/// </summary>
public sealed record LeaseEscrowSummary(
    decimal Amount,
    string Currency,
    string Status,
    decimal PayoutAmount,
    int PayoutAttempts,
    string? FailureReason,
    DateTimeOffset? OccurredAt);

public sealed record LeasePartyItem(Guid Id, string Name, string? Email);

public sealed record InspectionListItem(
    Guid Id,
    string Status,
    DateTimeOffset? PreferredDate,
    DateTimeOffset? ScheduledDate,
    string? Note,
    DateTimeOffset? ActualDate = null,
    string? Notes = null,
    DateTimeOffset? ProposedDate = null,
    string? RescheduleNote = null);