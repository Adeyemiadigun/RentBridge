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
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Atomically claims an escrow payment for payout: flips it to Releasing and
        /// stamps the payout reference in a single conditional UPDATE, but only when
        /// it is currently Funded or PayoutFailed. Returns true only for the caller
        /// that won the claim — a concurrent attempt sees 0 rows and must not transfer.
        /// A DB-level guard against double payouts.
        /// </summary>
        Task<bool> TryClaimEscrowPayoutAsync(Guid escrowPaymentId, string payoutReference, CancellationToken cancellationToken);
    }
}
