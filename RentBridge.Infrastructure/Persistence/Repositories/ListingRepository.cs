using Microsoft.EntityFrameworkCore;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Infrastructure.Persistence.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly AppDbContext _context;

    public ListingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ListingSearchItem>> SearchAsync(
        string? state,
        string? city,
        string? area,
        decimal? minPrice,
        decimal? maxPrice,
        ListingStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var resolvedStatus = status ?? ListingStatus.Published;

        // Project an anonymous shape in SQL (EF Core cannot translate the
        // ListingSearchItem record constructor, which surfaced as a 500 on
        // GET /listings/search). The record is built in memory afterwards.
        var query = _context.Set<Listing>().AsNoTracking()
            .Include(listing => listing.Images)
            .Where(listing => listing.Status == resolvedStatus
                && (minPrice == null || listing.Price.Amount >= minPrice)
                && (maxPrice == null || listing.Price.Amount <= maxPrice))
            .Join(
                _context.Set<Property>().AsNoTracking()
                    .Where(property => (state == null || property.PropertyAddress.State == state)
                        && (city == null || property.PropertyAddress.City == city)
                        && (area == null || property.PropertyAddress.Area == area)),
                listing => listing.PropertyId,
                property => property.Id,
                (listing, property) => new { listing, property })
            .OrderByDescending(x => x.listing.PublishedAt);

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(x => new ListingSearchItem(
                x.listing.Id,
                x.listing.Title,
                x.listing.Description,
                x.listing.Price.Amount,
                x.listing.Price.Currency,
                x.listing.Status,
                x.listing.CoverImageKey,
                x.listing.Images.OrderBy(img => img.Position).Select(img => img.Url).ToList(),
                x.listing.CreatedAt,
                x.listing.PublishedAt,
                x.listing.PropertyId,
                x.listing.ListingType,
                x.listing.PaymentPlan,
                x.listing.CautionFee?.Amount,
                x.listing.RealHouseFee?.Amount,
                x.listing.AgentFee?.Amount,
                x.property.PropertyAddress.Street,
                x.property.PropertyAddress.City,
                x.property.PropertyAddress.Area,
                x.property.PropertyAddress.State,
                x.property.PropertyType,
                x.property.Bedrooms,
                x.property.Bathrooms,
                x.property.Amenities))
            .ToList();

        return new PagedResult<ListingSearchItem>(page, pageSize, totalCount, items);
    }

    public async Task<ListingDetailItem?> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var query = _context.Set<Listing>().AsNoTracking()
            .Include(listing => listing.Images)
            .Where(listing => listing.Id == id && listing.Status == ListingStatus.Published)
            .Join(
                _context.Set<Property>().AsNoTracking(),
                listing => listing.PropertyId,
                property => property.Id,
                (listing, property) => new { listing, property })
            .Join(
                _context.Set<User>().AsNoTracking(),
                x => x.listing.OwnerUserId,
                user => user.Id,
                (x, user) => new { x.listing, x.property, user });

        var row = await query.FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var l = row.listing;
        var p = row.property;
        var u = row.user;
        return new ListingDetailItem(
            l.Id,
            l.Title,
            l.Description,
            l.Price.Amount,
            l.Price.Currency,
            l.Status,
            l.CoverImageKey,
            l.Images.OrderBy(img => img.Position).Select(img => img.Url).ToList(),
            l.CreatedAt,
            l.PublishedAt,
            l.PropertyId,
            l.ListingType,
            l.PaymentPlan,
            l.CautionFee?.Amount,
            l.RealHouseFee?.Amount,
            l.AgentFee?.Amount,
            p.PropertyAddress.Street,
            p.PropertyAddress.City,
            p.PropertyAddress.Area,
            p.PropertyAddress.State,
            p.PropertyType,
            p.Bedrooms,
            p.Bathrooms,
            p.AvailableFrom,
            p.Amenities,
            u.Id,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.Email.Value,
            u.IdentityVerified);
    }
}