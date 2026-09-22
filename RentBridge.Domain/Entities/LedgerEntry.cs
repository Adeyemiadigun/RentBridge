using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Entities;

/// <summary>
/// An append-only financial ledger line. A row is written as each money event
/// happens (rent funded, fees taken, payout sent, payout failed) and is never
/// updated or deleted, so the table is the authoritative audit trail for escrow.
/// Amounts are denormalized so the history can be filtered, sorted and paged
/// entirely in the database.
/// </summary>
public class LedgerEntry : Entity<Guid>
{
    public Guid LeaseId { get; private set; }
    public Guid EscrowPaymentId { get; private set; }

    /// <summary>Tenant on the lease — the party who funded escrow.</summary>
    public Guid TenantUserId { get; private set; }

    /// <summary>Owner paid out on the lease.</summary>
    public Guid LandlordUserId { get; private set; }

    public TransactionType Type { get; private set; }
    public TransactionDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    /// <summary>Provider reference this line relates to (charge or payout reference).</summary>
    public string? Reference { get; private set; }

    /// <summary>Escrow status at the moment the line was written.</summary>
    public EscrowStatus Status { get; private set; }

    /// <summary>Payout attempt this line relates to (0 for non-payout lines).</summary>
    public int Attempt { get; private set; }

    public string Description { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LedgerEntry()
    {
        Currency = string.Empty;
        Description = string.Empty;
    }

    public LedgerEntry(
        Guid leaseId,
        Guid escrowPaymentId,
        Guid tenantUserId,
        Guid landlordUserId,
        TransactionType type,
        TransactionDirection direction,
        decimal amount,
        string currency,
        EscrowStatus status,
        string description,
        string? reference = null,
        int attempt = 0)
    {
        Id = Guid.NewGuid();
        LeaseId = leaseId;
        EscrowPaymentId = escrowPaymentId;
        TenantUserId = tenantUserId;
        LandlordUserId = landlordUserId;
        Type = type;
        Direction = direction;
        Amount = amount;
        Currency = currency;
        Status = status;
        Description = description;
        Reference = reference;
        Attempt = attempt;
        OccurredAt = DateTimeOffset.UtcNow;
        CreatedAt = OccurredAt;
    }
}
