using RentBridge.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RentBridge.Application.Common.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<T?> FirstOrDefault(Expression<Func<T, bool>> predicate, CancellationToken ct);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken ct);
        Task<PagedResult<T>> GetPagedAsync(
            Expression<Func<T, bool>>? predicate,
            int page,
            int pageSize,
            Expression<Func<T, object?>>? orderBy = null,
            bool ascending = true,
            CancellationToken ct = default,
            params Expression<Func<T, object?>>[]? includes);
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
    }
}
