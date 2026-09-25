using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Common.Interfaces.Repositories;

public interface IListingRepository
{
    Task<PagedResult<ListingSearchItem>> SearchAsync(
        string? state,
        string? city,
        string? area,
        decimal? minPrice,
        decimal? maxPrice,
        ListingStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Returns a published listing composed with its property and the
    /// owning user's details, or null when not found / not published.
    /// </summary>
    Task<ListingDetailItem?> GetDetailAsync(Guid id, CancellationToken ct);
}