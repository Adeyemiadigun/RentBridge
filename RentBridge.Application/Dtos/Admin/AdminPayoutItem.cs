using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Admin;

/// <summary>
/// One row of the admin payouts console: the latest escrow payout for a lease,
/// with everything an admin needs to see why it is stuck and to retry it.
/// </summary>
public sealed record AdminPayoutItem(
    Guid LeaseId,
    Guid ListingId,
    string PropertyTitle,
    string Location,
    string LandlordName,
    string TenantName,
    decimal PayoutAmount,
    string Currency,
    EscrowStatus Status,
    int PayoutAttempts,
    string? FailureReason,
    DateTimeOffset? FailedAt,
    DateTimeOffset? PayoutStartedAt,
    string? PayoutReference,
    bool CanRetry,
    DateTimeOffset UpdatedAt);
