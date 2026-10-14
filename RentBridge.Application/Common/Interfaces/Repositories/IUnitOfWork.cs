using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Common.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IGenericRepository<T> Repository<T>() where T : Entity<Guid>;
        IListingRepository Listings { get; }
        ILeaseRepository Leases { get; }
        ILedgerRepository Ledger { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Atomically claims an escrow payment for payout: flips it to Releasing and
        /// stamps the payout reference in a single conditional UPDATE, but only when
        /// it is currently Funded or PayoutFailed. Returns true only for the caller
        /// that won the claim — a concurrent attempt sees 0 rows and must not transfer.
        /// A DB-level guard against double payouts.
        /// </summary>
        Task<bool> TryClaimEscrowPayoutAsync(Guid escrowPaymentId, string payoutReference, CancellationToken cancellationToken);

        /// <summary>
        /// Marks a newly created entity as Added. Children added to the collection of an
        /// already-tracked aggregate are otherwise picked up as Modified, which makes EF
        /// emit an UPDATE against a row that does not exist yet.
        /// </summary>
        void MarkAsAdded<T>(T entity) where T : class;

        /// <summary>
        /// Accepts the entity's current values as the persisted baseline so the next
        /// SaveChanges only writes what changes afterwards. Required after a raw-SQL
        /// update (e.g. <see cref="TryClaimEscrowPayoutAsync"/>) mutates a tracked row
        /// behind EF's back: without it, a value that is changed in memory and then
        /// changed back to its pre-claim value compares equal to the stale OriginalValue
        /// and is silently omitted from the UPDATE, leaving the DB row on the raw-SQL value.
        /// </summary>
        void AcceptChanges(object entity);

        /// <summary>
        /// Clears the EF Core change tracker to force fresh entity loads on retry.
        /// </summary>
        void ClearChangeTracker();
    }
}
