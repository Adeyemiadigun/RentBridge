using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Drives escrow payout to the landlord's registered account without any
/// seller action. Safe to call repeatedly: it only pays out once all three
/// verification gates have passed and the escrow is funded, and it never
/// double-pays an in-flight or completed transfer.
///
/// Trigger policy: the automatic payout is driven ONLY by the funding-success
/// webhook (money confirmed received) plus the reconciliation/retry jobs and the
/// admin force-release. Intermediate flow steps (inspection confirmed/completed,
/// agreement certified, identity verified) deliberately do NOT attempt a release:
/// at those points the agreement is unsigned or unfunded, so a payout could not
/// legitimately happen, and firing the check from there only produced misleading
/// log noise and non-deterministic "which trigger moved the money" behaviour.
/// </summary>
public interface IEscrowReleaseService
{
    /// <summary>
    /// Attempts the payout for a lease if escrow is funded and all three gates
    /// (identity, inspection, legal) are stamped. A no-op returning
    /// <c>Result.Ok()</c> means "not releasable yet" — it does not signal failure.
    /// </summary>
    Task<Result> TryAutoReleaseAsync(Guid leaseId, CancellationToken cancellationToken);

    /// <summary>
    /// Retry entry point used by the admin payouts console. Same gates and
    /// idempotency rules as <see cref="TryAutoReleaseAsync"/>; the attempt counter
    /// is incremented, never reset.
    /// </summary>
    Task RetryAutoReleaseAsync(Guid leaseId);

    /// <summary>
    /// Records an out-of-band payout failure (e.g. a transfer.failed webhook):
    /// marks the payment payout-failed with the provider's <paramref name="reason"/>,
    /// notifies the owner, and leaves the payout awaiting a manual admin retry.
    /// It is never retried automatically.
    /// </summary>
    Task HandlePayoutFailureAsync(Guid leaseId, string? reason, CancellationToken cancellationToken);

    /// <summary>
    /// Reconciliation sweep for payouts that were claimed (Releasing) but never
    /// finalized — e.g. the process died after the claim and before the provider
    /// call, so no webhook will ever arrive. Asks the provider what actually
    /// happened and either finalizes the payout, records the failure, or re-drives
    /// it. Safe to run on a recurring schedule.
    /// </summary>
    Task ReconcileStuckPayoutsAsync();
}
