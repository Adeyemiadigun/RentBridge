using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Aggregates;
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
        CancellationToken ct,
        Guid? ownerUserId = null);

    /// <summary>
    /// Returns a published listing composed with its property and the
    /// owning user's details, or null when not found / not published.
    /// </summary>
    Task<ListingDetailItem?> GetDetailAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Counts listings grouped by status, skipping any listing whose property has
    /// been deleted. Deleting a property does not cascade (the Listing.PropertyId
    /// foreign key is unenforced), so a plain GROUP BY on listings counts those
    /// orphans forever and the admin KPIs never go down.
    /// </summary>
    Task<IReadOnlyList<GroupCount<ListingStatus>>> CountByStatusWithLivePropertyAsync(
        CancellationToken ct);

    /// <summary>
    /// Pages listings joined to their property so rows orphaned by a property delete
    /// never enter a page. Filtering after paging would still let orphans consume page
    /// slots and corrupt <c>TotalCount</c>.
    /// </summary>
    Task<(IReadOnlyList<Listing> Items, int TotalCount)> GetLivePagedAsync(
        ListingStatus? status,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Ids of listings whose property row no longer exists — the orphans left behind
    /// before property deletes cascaded. Ids only, so a maintenance sweep can size the
    /// damage before hydrating anything.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetIdsWithoutLivePropertyAsync(CancellationToken ct);

    /// <summary>
    /// Loads listings tracked so a caller can delete them; EF then removes the owned
    /// listing_images rows with them.
    /// </summary>
    Task<IReadOnlyList<Listing>> GetTrackedByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct);
}