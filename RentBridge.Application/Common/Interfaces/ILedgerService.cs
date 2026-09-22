using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Entities;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Writes append-only ledger lines for escrow money events. Implementations
/// only stage the entries; the caller's unit of work commits them, so a ledger
/// line and the state change it describes are saved atomically.
/// </summary>
public interface ILedgerService
{
    /// <summary>Records rent paid into escrow (one line).</summary>
    Task RecordFundingAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken);

    /// <summary>Records the released split: commission, legal fee, and net payout.</summary>
    Task RecordReleaseAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken);

    /// <summary>Records a payout attempt that did not complete.</summary>
    Task RecordPayoutFailureAsync(Lease lease, EscrowPayment payment, CancellationToken cancellationToken);
}
