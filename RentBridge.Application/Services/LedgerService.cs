using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Services;

/// <summary>
/// Stages ledger lines for the escrow lifecycle. Every write is guarded by an
/// existence check on (payment, type, attempt); combined with the unique index
/// on the same columns this makes a replayed funding/payout event a no-op
/// instead of a duplicate line.
/// </summary>
public sealed class LedgerService(IUnitOfWork unitOfWork) : ILedgerService
{
    public Task RecordFundingAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken)
        => AddIfAbsentAsync(
            lease,
            payment,
            TransactionType.EscrowFunded,
            TransactionDirection.Credit,
            payment.GrossAmount.Amount,
            payment.GrossAmount.Currency,
            EscrowStatus.Funded,
            attempt: 0,
            description: "Rent funded into escrow",
            reference: payment.Reference,
            cancellationToken);

    public async Task RecordReleaseAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken)
    {
        var split = payment.Split;
        if (split is null)
        {
            return;
        }

        var reference = payment.PayoutReference ?? payment.Reference;

        await AddIfAbsentAsync(
            lease, payment, TransactionType.PlatformCommission, TransactionDirection.Debit,
            split.PlatformCommission.Amount, split.PlatformCommission.Currency,
            EscrowStatus.Released, attempt: 0, description: "Platform commission",
            reference, cancellationToken);

        await AddIfAbsentAsync(
            lease, payment, TransactionType.LegalFeeShare, TransactionDirection.Debit,
            split.LegalFeeShare.Amount, split.LegalFeeShare.Currency,
            EscrowStatus.Released, attempt: 0, description: "Legal fee",
            reference, cancellationToken);

        await AddIfAbsentAsync(
            lease, payment, TransactionType.LandlordPayout, TransactionDirection.Credit,
            split.LandlordPayout.Amount, split.LandlordPayout.Currency,
            EscrowStatus.Released, attempt: 0, description: "Landlord payout (net of fees)",
            reference, cancellationToken);
    }

    public Task RecordPayoutFailureAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken)
    {
        // A failed payout moves no money — the landlord's funds are still held in
        // escrow. The line is recorded with a ZERO amount on purpose: recording the
        // payout amount made every retry look like fresh money (₦6.30 × attempts)
        // and double-counted the same funds. The pending amount lives on the escrow
        // payment; this line is only an attempt marker (attempt number + reason).
        return AddIfAbsentAsync(
            lease,
            payment,
            TransactionType.PayoutFailed,
            TransactionDirection.Info,
            amount: 0m,
            payment.GrossAmount.Currency,
            EscrowStatus.PayoutFailed,
            attempt: payment.PayoutAttempts,
            description: $"Landlord payout attempt {payment.PayoutAttempts} failed",
            reference: payment.PayoutReference ?? payment.Reference,
            cancellationToken);
    }

    private async Task AddIfAbsentAsync(
        Lease lease,
        EscrowPayment payment,
        TransactionType type,
        TransactionDirection direction,
        decimal amount,
        string currency,
        EscrowStatus status,
        int attempt,
        string description,
        string? reference,
        CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Repository<LedgerEntry>();

        var exists = await repository.AnyAsync(
            e => e.EscrowPaymentId == payment.Id && e.Type == type && e.Attempt == attempt,
            cancellationToken);
        if (exists)
        {
            return;
        }

        repository.Add(new LedgerEntry(
            lease.Id,
            payment.Id,
            lease.TenantUserId,
            lease.LandlordUserId,
            type,
            direction,
            amount,
            currency,
            status,
            description,
            reference,
            attempt));
    }
}
