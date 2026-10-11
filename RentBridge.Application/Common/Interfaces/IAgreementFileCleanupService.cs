using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Reclaims object-storage bytes for agreement files that have been superseded
/// (a re-upload, or the intermediate files released when the flow is finalized).
/// Deleting the bytes and dropping the row are done together, and every step is
/// idempotent so a retry can never fail on an already-deleted asset.
/// </summary>
public interface IAgreementFileCleanupService
{
    /// <summary>
    /// Deletes the bytes of every file on the lease's agreement flagged for
    /// deletion, then drops the row. Best-effort and partial-safe: files whose
    /// bytes could not be deleted stay flagged so a later pass retries them.
    /// Does NOT call SaveChanges — the caller owns the unit of work.
    /// </summary>
    Task<Result> PurgePendingDeletionsAsync(LeaseAggregate lease, CancellationToken cancellationToken);

    /// <summary>
    /// Reconciliation sweep for agreements left holding files flagged for
    /// deletion — e.g. the process died between the approval save and the
    /// storage delete. Safe to run on a recurring schedule.
    /// </summary>
    Task ReconcilePendingDeletionsAsync();
}
