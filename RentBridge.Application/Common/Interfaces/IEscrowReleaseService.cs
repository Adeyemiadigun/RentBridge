using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Drives escrow payout to the landlord's registered account without any
/// seller action. Safe to call repeatedly: it only pays out once all three
/// verification gates have passed and the escrow is funded, and it never
/// double-pays an in-flight or completed transfer.
/// </summary>
public interface IEscrowReleaseService
{
    Task<Result> TryAutoReleaseAsync(Guid leaseId, CancellationToken cancellationToken);

    /// <summary>
    /// Job-scheduler-friendly entry point (single serializable argument) used
    /// for bounded automatic retries after a failed payout.
    /// </summary>
    Task RetryAutoReleaseAsync(Guid leaseId);

    /// <summary>
    /// Records an out-of-band payout failure (e.g. a transfer.failed webhook):
    /// marks the payment payout-failed, notifies the owner, and schedules a
    /// bounded automatic retry. No-op if the payment is unknown or already done.
    /// </summary>
    Task HandlePayoutFailureAsync(Guid leaseId, CancellationToken cancellationToken);

    /// <summary>
    /// Reconciliation sweep for payouts that were claimed (Releasing) but never
    /// finalized — e.g. the process died after the claim and before the provider
    /// call, so no webhook will ever arrive. Asks the provider what actually
    /// happened and either finalizes the payout, records the failure, or re-drives
    /// it. Safe to run on a recurring schedule.
    /// </summary>
    Task ReconcileStuckPayoutsAsync();
}
