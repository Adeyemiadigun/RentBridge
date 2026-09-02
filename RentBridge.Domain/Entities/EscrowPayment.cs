using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates.Users;

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
        if (Status != EscrowStatus.Funded) return Result.Fail("Only funded escrow can begin releasing.");
        Status = EscrowStatus.Releasing;
        return Result.Ok();
    }

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
}
