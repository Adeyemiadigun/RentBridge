using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;
using UserAggregate = RentBridge.Domain.Aggregates.User;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Query.Lease;

public sealed class GetCallerLeasesQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetCallerLeasesQueryHandler> logger)
    : IRequestHandler<GetCallerLeasesQuery, Result<PagedResult<LeaseListItem>>>
{
    public async Task<Result<PagedResult<LeaseListItem>>> Handle(
        GetCallerLeasesQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PagedResult<LeaseListItem>>.Fail(res.Error!);
        }
        var user = res.Value;
        var isAdmin = user.Role is UserRole.Admin;

        var paged = await unitOfWork.Repository<LeaseAggregate>().GetPagedAsync(
            l => (isAdmin
                || l.LandlordUserId == user.Id
                || l.TenantUserId == user.Id
                || l.AssignedLawyerId == user.Id)
                && l.ListingId != Guid.Empty,
            request.Page,
            request.PageSize,
            l => l.CreatedAt,
            ascending: false,
            cancellationToken,
            l => l.InspectionRequests);

        var leases = paged.Items;

        var listingIds = leases.Select(l => l.ListingId).Distinct().ToList();
        var userIds = leases
            .SelectMany(l => new Guid?[] { l.LandlordUserId, l.TenantUserId, l.AssignedLawyerId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var listingsById = listingIds.Count == 0
            ? new Dictionary<Guid, ListingAggregate>()
            : (await unitOfWork.Repository<ListingAggregate>()
                .FindAsync(x => listingIds.Contains(x.Id), cancellationToken))
                .ToDictionary(x => x.Id);

        var usersById = userIds.Count == 0
            ? new Dictionary<Guid, UserAggregate>()
            : (await unitOfWork.Repository<UserAggregate>()
                .FindAsync(x => userIds.Contains(x.Id), cancellationToken))
                .ToDictionary(x => x.Id);

        // Get properties for listings to filter out deleted properties
        var propertyIds = listingsById.Values
            .Where(l => l.PropertyId != Guid.Empty)
            .Select(l => l.PropertyId)
            .Distinct()
            .ToList();

        var propertiesById = propertyIds.Count == 0
            ? new Dictionary<Guid, PropertyAggregate>()
            : (await unitOfWork.Repository<PropertyAggregate>()
                .FindAsync(x => propertyIds.Contains(x.Id), cancellationToken))
                .ToDictionary(x => x.Id);

        LeasePartyItem? Party(Guid id)
        {
            if (!usersById.TryGetValue(id, out var u)) return null;
            return new LeasePartyItem(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email.Value);
        }

        var items = leases
            .Where(l => listingsById.TryGetValue(l.ListingId, out var listing) && listing.PropertyId != Guid.Empty && propertiesById.ContainsKey(listing.PropertyId))
            .Select(l =>
            {
                var listing = listingsById[l.ListingId];
                var property = propertiesById[listing.PropertyId];
                return new LeaseListItem(
                    l.Id,
                    l.ListingId,
                    listing?.Title,
                    l.Status.ToString(),
                    l.CreatedAt,
                    null,
                    Party(l.TenantUserId),
                    Party(l.LandlordUserId),
                    l.AssignedLawyerId is Guid lawyerId ? Party(lawyerId) : null,
                    l.InspectionRequests
                        .OrderByDescending(r => r.PreferredDate)
                    .Select(r => new InspectionListItem(
                        r.Id,
                        r.Status.ToString(),
                        r.PreferredDate,
                        r.ScheduledDate,
                        string.IsNullOrWhiteSpace(r.Note) ? null : r.Note,
                        r.ActualDate,
                        string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes))
                    .FirstOrDefault());
        }).ToList();

        return Result<PagedResult<LeaseListItem>>.Ok(
            new PagedResult<LeaseListItem>(paged.Page, paged.PageSize, paged.TotalCount, items));
    }
}