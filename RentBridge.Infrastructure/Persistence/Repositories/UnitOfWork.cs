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

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IListingRepository Listings => _listingRepository ??= new ListingRepository(_context);

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
    }
}
