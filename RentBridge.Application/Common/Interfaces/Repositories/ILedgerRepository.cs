using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Entities;
using System.Linq.Expressions;

namespace RentBridge.Application.Common.Interfaces.Repositories
{
    /// <summary>
    /// Read-only ledger analytics. Totals are grouped server-side so dashboard
    /// metrics never pull individual lines into memory.
    /// </summary>
    public interface ILedgerRepository
    {
        Task<IReadOnlyList<LedgerTypeTotal>> GetTotalsAsync(
            Expression<Func<LedgerEntry, bool>>? predicate,
            CancellationToken ct);

        /// <summary>
        /// Time-bucketed transaction series, grouped in SQL by UTC calendar
        /// day / Monday-start week / calendar month. Only buckets containing
        /// lines are returned; callers fill gaps. The `to` bound is exclusive.
        /// </summary>
        Task<IReadOnlyList<TransactionMetricsPoint>> GetTransactionMetricsAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            MetricsGranularity granularity,
            Guid? landlordUserId,
            CancellationToken ct);

        /// <summary>
        /// Returns the distinct lease ids that have at least one ledger line. Used to
        /// protect financially material records from destructive maintenance: any lease
        /// that moved money must never be silently purged, even when orphaned.
        /// </summary>
        Task<IReadOnlyList<Guid>> GetLeaseIdsWithLedgerEntriesAsync(
            IReadOnlyCollection<Guid> leaseIds,
            CancellationToken ct);
    }
}
