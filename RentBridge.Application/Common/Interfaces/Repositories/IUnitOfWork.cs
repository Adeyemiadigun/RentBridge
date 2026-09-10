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
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
