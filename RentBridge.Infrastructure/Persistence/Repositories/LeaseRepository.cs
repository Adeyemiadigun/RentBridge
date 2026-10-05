using Microsoft.EntityFrameworkCore;
using RentBridge.Application.Common;
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
    /// Loads a tracked lease with every owned/related collection that the agreement
    /// and escrow paths read: the agreement signatures, the agreement document, and
    /// the escrow payments.
    ///
    /// None of these load unless EF is explicitly told to. Signatures and the document
    /// are owned types and EscrowPayments is a related collection, so a bare
    /// FirstOrDefault hands back an entity whose collections are all EMPTY and whose
    /// document is null — silently, with no error. That made GET /leases/{id} report
    /// isFullySigned=false and zero signatures even after both parties had signed,
    /// which made the UI try to sign a third time and the escrow path reject a
    /// legitimate payment. Anything mapping a Lease to a DTO must use this.
    ///
    /// FindAsync is used rather than a query so shadow properties (Version/xmin) are
    /// materialised; a plain query would leave them at CLR default and break the
    /// optimistic-concurrency check on save.
    /// </summary>
    public async Task<Lease?> GetWithAgreementGraphAsync(
        Guid leaseId,
        CancellationToken ct)
    {
        var entity = await _context.Set<Lease>().FindAsync([leaseId], ct);
        if (entity is null)
            return null;

        // Agreement is itself an owned reference on Lease; reach its entry before
        // loading what hangs off it.
        var agreementEntry = _context.Entry(entity).Reference(l => l.Agreement).TargetEntry;
        if (agreementEntry is not null)
        {
            await agreementEntry.Collection(a => a.Signatures).LoadAsync(ct);
            await agreementEntry.Reference(a => a.Document).LoadAsync(ct);
        }

        await _context.Entry(entity).Collection(l => l.EscrowPayments).LoadAsync(ct);
        return entity;
    }

    /// <summary>
    /// Resolves a lease from a Paystack reference (either the funding reference or
    /// the payout reference) and loads it via <see cref="GetWithAgreementGraphAsync"/>.
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
            : await GetWithAgreementGraphAsync(leaseId.Value, ct);
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

    public async Task<IReadOnlyList<GroupCount<LeaseStatus>>> CountByStatusWithLivePropertyAsync(
        CancellationToken ct)
    {
        var rows = await (
            from lease in _context.Set<Lease>().AsNoTracking()
            join listing in _context.Set<Listing>().AsNoTracking()
                on lease.ListingId equals listing.Id
            join property in _context.Set<Property>().AsNoTracking()
                on listing.PropertyId equals property.Id
            group lease by lease.Status
            into g
            select new { Key = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => new GroupCount<LeaseStatus>(r.Key, r.Count)).ToList();
    }

    public async Task<IReadOnlyList<Lease>> GetByListingIdsWithEscrowPaymentsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken ct)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        var ids = listingIds.ToList();
        return await _context.Set<Lease>()
            .Include(l => l.EscrowPayments)
            .Include(l => l.InspectionRequests)
            .Include(l => l.Agreement).ThenInclude(a => a.Signatures)
            .Include(l => l.Agreement).ThenInclude(a => a.Document)
            .Where(l => ids.Contains(l.ListingId))
            .ToListAsync(ct);
    }
}
