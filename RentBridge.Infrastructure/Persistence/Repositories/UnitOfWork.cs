using Microsoft.EntityFrameworkCore;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private Hashtable? _repositories;
        private IListingRepository? _listingRepository;
        private ILeaseRepository? _leaseRepository;
        private ILedgerRepository? _ledgerRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IListingRepository Listings => _listingRepository ??= new ListingRepository(_context);

        public ILeaseRepository Leases => _leaseRepository ??= new LeaseRepository(_context);

        public ILedgerRepository Ledger => _ledgerRepository ??= new LedgerRepository(_context);

        public IGenericRepository<T> Repository<T>() where T : Entity<Guid>
        {
            _repositories ??= new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryInstance = new GenericRepository<T>(_context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TryClaimEscrowPayoutAsync(
            Guid escrowPaymentId,
            string payoutReference,
            CancellationToken cancellationToken)
        {
            // Conditional UPDATE is race-safe under Read Committed: concurrent claims
            // serialize on the row lock, and the loser re-evaluates the WHERE against
            // the already-flipped status and matches zero rows.
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE escrow_payments
                SET "Status" = 'Releasing',
                    "PayoutReference" = {payoutReference},
                    "payout_started_at" = now()
                WHERE "Id" = {escrowPaymentId}
                  AND "Status" IN ('Funded', 'PayoutFailed')
                """, cancellationToken);

            return affected == 1;
        }
    }
}
