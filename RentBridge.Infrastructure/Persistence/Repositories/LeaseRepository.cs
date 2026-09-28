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

    public async Task<Lease?> GetWithInspectionRequestsAsync(
        Guid leaseId,
        CancellationToken ct)
    {
        // Step 1: Load lease by PK (FindAsync properly loads all shadow properties including Version/xmin)
        var entity = await _context.Set<Lease>().FindAsync([leaseId], ct);
        if (entity is null)
            return null;

        // Step 2: Explicitly load InspectionRequests collection
        await _context.Entry(entity).Collection(l => l.InspectionRequests).LoadAsync(ct);
        
        // Verify Version/xmin was loaded
        var versionVal = _context.Entry(entity).Property("Version").CurrentValue;
        System.Diagnostics.Debug.WriteLine($"[DEBUG REPO] Loaded lease {entity.Id}, Version={versionVal}");
        
        return entity;
    }
}
