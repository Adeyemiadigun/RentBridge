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

        var query = _context.Set<Listing>().AsNoTracking()
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
                (listing, property) => new ListingSearchItem(
                    listing.Id,
                    listing.Title,
                    listing.Description,
                    listing.Price.Amount,
                    listing.Price.Currency,
                    listing.Status,
                    listing.CoverImageKey,
                    listing.CreatedAt,
                    listing.PublishedAt,
                    listing.PropertyId,
                    property.PropertyAddress.Street,
                    property.PropertyAddress.City,
                    property.PropertyAddress.Area,
                    property.PropertyAddress.State))
            .OrderByDescending(x => x.PublishedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ListingSearchItem>(page, pageSize, totalCount, items);
    }
}