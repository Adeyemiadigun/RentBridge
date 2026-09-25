using Microsoft.EntityFrameworkCore;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RentBridge.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _dbSet.FindAsync([id], ct);
        }

        public async Task<T?> FirstOrDefault(Expression<Func<T, bool>> predicate, CancellationToken ct)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, ct);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct)
        {
            return await _dbSet.AsNoTracking().ToListAsync(ct);
        }

        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct)
        {
            return await _dbSet.AnyAsync(predicate, ct);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken ct)
        {
            return predicate == null
                ? await _dbSet.CountAsync(ct)
                : await _dbSet.CountAsync(predicate, ct);
        }

        public async Task<IReadOnlyList<GroupCount<TKey>>> CountByAsync<TKey>(
            Expression<Func<T, bool>>? predicate,
            Expression<Func<T, TKey>> keySelector,
            CancellationToken ct)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            // Anonymous projection first: keeps EF translation bulletproof,
            // then map to the record client-side.
            var rows = await query
                .GroupBy(keySelector)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return rows.Select(r => new GroupCount<TKey>(r.Key, r.Count)).ToList();
        }

        public async Task<PagedResult<T>> GetPagedAsync(
            Expression<Func<T, bool>>? predicate,
            int page,
            int pageSize,
            Expression<Func<T, object?>>? orderBy = null,
            bool ascending = true,
            CancellationToken ct = default,
            params Expression<Func<T, object?>>[]? includes)
        {
            var query = _dbSet.AsNoTracking();

            if (includes is { Length: > 0 })
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (orderBy is not null)
            {
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<T>(page, pageSize, totalCount, items);
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _dbSet.AddRange(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
    }

}
