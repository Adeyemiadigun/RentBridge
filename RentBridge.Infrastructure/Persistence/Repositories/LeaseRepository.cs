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

    /// <summary>
    /// Loads a tracked lease with the collections the escrow release path depends on:
    /// the agreement signatures and the escrow payments.
    ///
    /// Signatures live in their own table via OwnsMany, and EF never loads an owned
    /// collection unless it is explicitly asked to. A bare FirstOrDefault therefore
    /// returns an Agreement whose _signatures collection is EMPTY, which made
    /// Agreement.IsFullySigned report false even after both parties had signed — so
    /// RecordFunding rejected every payment webhook and escrow could never be funded.
    /// The payment path must go through here.
    ///
    /// FindAsync is used rather than a query so shadow properties (Version/xmin) are
    /// materialised; a plain query would leave them at CLR default and break the
    /// optimistic-concurrency check on save.
    /// </summary>
    public async Task<Lease?> GetForEscrowSettlementAsync(
        Guid leaseId,
        CancellationToken ct)
    {
        var entity = await _context.Set<Lease>().FindAsync([leaseId], ct);
        if (entity is null)
            return null;

        // Agreement is itself an owned reference on Lease; reach its entry before
        // loading the signatures collection that hangs off it.
        var agreementEntry = _context.Entry(entity).Reference(l => l.Agreement).TargetEntry;
        if (agreementEntry is not null)
        {
            await agreementEntry.Collection(a => a.Signatures).LoadAsync(ct);
        }

        await _context.Entry(entity).Collection(l => l.EscrowPayments).LoadAsync(ct);
        return entity;
    }

    /// <summary>
    /// Resolves a lease from a Paystack reference (either the funding reference or
    /// the payout reference) and loads it via <see cref="GetForEscrowSettlementAsync"/>.
    /// </summary>
    public async Task<Lease?> GetForEscrowSettlementByReferenceAsync(
        string reference,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        // Resolve the id with an untracked projection, then re-load tracked so the
        // owned collections and shadow concurrency token come back populated.
        var leaseId = await _context.Set<Lease>()
            .Where(l => l.EscrowPayments.Any(p =>
                p.Reference == reference || p.PayoutReference == reference))
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct);

        return leaseId is null
            ? null
            : await GetForEscrowSettlementAsync(leaseId.Value, ct);
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
        
        return entity;
    }
}
