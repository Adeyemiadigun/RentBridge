using Microsoft.EntityFrameworkCore;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Infrastructure.Persistence.Repositories;

public class LeaseRepository : ILeaseRepository
{
    private readonly AppDbContext _context;

    public LeaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Lease>> GetWithStuckPayoutsAsync(
        DateTimeOffset cutoff,
        CancellationToken ct)
    {
        return await _context.Set<Lease>()
            .Where(l => l.EscrowPayments.Any(p =>
                p.Status == EscrowStatus.Releasing
                && p.PayoutStartedAt != null
                && p.PayoutStartedAt < cutoff))
            .ToListAsync(ct);
    }
}
