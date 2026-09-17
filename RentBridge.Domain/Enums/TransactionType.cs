namespace RentBridge.Domain.Enums;

/// <summary>
/// The kind of money movement a ledger line represents. The word "Fee" here is
/// intentionally not used for the platform/legal lines: each is an explicit
/// ledger event so the split is auditable line-by-line.
/// </summary>
public enum TransactionType
{
    /// <summary>Rent paid by the tenant into escrow.</summary>
    EscrowFunded,

    /// <summary>Platform commission deducted from the gross escrow amount.</summary>
    PlatformCommission,

    /// <summary>Legal fee share deducted from the gross escrow amount.</summary>
    LegalFeeShare,

    /// <summary>Net amount paid out to the property owner.</summary>
    LandlordPayout,

    /// <summary>A payout attempt that did not complete (informational).</summary>
    PayoutFailed,
}
