using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Owned child entity of the Lease aggregate. Not an aggregate root —
/// an escrow payment only exists within its Lease. Persisted via the
/// Lease's OwnsMany mapping.
/// </summary>
public class EscrowPayment
{
    public Guid Id { get; private set; }
    public Guid LeaseId { get; private set; }
    public Guid UserId { get; private set; }
    public Money GrossAmount { get; private set; }
    public FeeSplit? Split { get; private set; }
    public string Reference { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public EscrowStatus Status { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? PayoutReference { get; private set; }
    public int PayoutAttempts { get; private set; }
    public DateTimeOffset? PayoutStartedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private EscrowPayment() { }

    public EscrowPayment(Guid leaseId, Guid userId, Money gross, string reference, Guid idempotencyKey)
    {
        Id = Guid.NewGuid();
        LeaseId = leaseId;
        UserId = userId;
        GrossAmount = gross;
        Reference = reference;
        IdempotencyKey = idempotencyKey;
        Status = EscrowStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Result AttachSplit(FeeSplit split)
    {
        Split = split;
        return Result.Ok();
    }

    public Result AttachCheckoutUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return Result.Fail("Checkout URL cannot be empty.");
        if (Status != EscrowStatus.Pending) return Result.Fail("Checkout URL can only be attached while pending.");
        CheckoutUrl = url;
        Status = EscrowStatus.Initialized;
        return Result.Ok();
    }

    public Result MarkFunded()
    {
        if (Status != EscrowStatus.Initialized) return Result.Fail("Payment must be initialized before it can be funded.");
        Status = EscrowStatus.Funded;
        return Result.Ok();
    }

    public Result MarkReleasing()
    {
        if (Status is not (EscrowStatus.Funded or EscrowStatus.PayoutFailed))
            return Result.Fail("Only funded or previously-failed escrow can begin releasing.");
        Status = EscrowStatus.Releasing;
        PayoutStartedAt = DateTimeOffset.UtcNow;
        return Result.Ok();
    }

    public Result MarkPayoutFailed()
    {
        if (Status is not (EscrowStatus.Releasing or EscrowStatus.Funded or EscrowStatus.PayoutFailed))
            return Result.Fail("Only releasing escrow can be marked payout-failed.");
        Status = EscrowStatus.PayoutFailed;
        return Result.Ok();
    }

    public void IncrementPayoutAttempt() => PayoutAttempts++;

    public void ResetPayoutAttempts() => PayoutAttempts = 0;

    public Result MarkReleased()
    {
        if (Status != EscrowStatus.Releasing) return Result.Fail("Escrow must be releasing before released.");
        Status = EscrowStatus.Released;
        return Result.Ok();
    }

    public Result MarkFailed()
    {
        Status = EscrowStatus.Failed;
        return Result.Ok();
    }

    public Result AttachPayoutReference(string providerReference)
    {
        if (string.IsNullOrWhiteSpace(providerReference)) return Result.Fail("Payout reference cannot be empty.");
        PayoutReference = providerReference;
        return Result.Ok();
    }

    /// <summary>
    /// Revives a pending or failed payment for a fresh funding attempt. Reuses the
    /// same row (so the per-lease idempotency key still guards against duplicate
    /// funding) but issues a brand-new reference and clears the stale checkout.
    /// </summary>
    public Result Reinitialize(string newReference)
    {
        if (Status is not (EscrowStatus.Failed or EscrowStatus.Pending))
            return Result.Fail("Only a pending or failed payment can be re-funded.");

        if (string.IsNullOrWhiteSpace(newReference))
            return Result.Fail("Reference cannot be empty.");

        Reference = newReference.Trim().ToUpperInvariant();
        Status = EscrowStatus.Pending;
        CheckoutUrl = null;
        PayoutReference = null;
        PayoutStartedAt = null;
        return Result.Ok();
    }
}
