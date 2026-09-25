using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Query.Admin;

public sealed class GetListingsForModerationQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetListingsForModerationQueryHandler> logger)
    : IRequestHandler<GetListingsForModerationQuery, Result<PagedResult<ListingModerationItem>>>
{
    public async Task<Result<PagedResult<ListingModerationItem>>> Handle(
        GetListingsForModerationQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PagedResult<ListingModerationItem>>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to list listings for moderation without admin role", actor.Id);
            return Result<PagedResult<ListingModerationItem>>.Fail("Only an admin can moderate listings.");
        }

        var page = await unitOfWork.Repository<ListingAggregate>().GetPagedAsync(
            l => request.Status == null || l.Status == request.Status,
            request.Page,
            request.PageSize,
            orderBy: l => l.CreatedAt,
            ascending: false,
            ct: cancellationToken);

        // One batch lookup for the whole page — no per-row queries.
        var propertyIds = page.Items.Select(l => l.PropertyId).Distinct().ToList();
        var properties = await unitOfWork.Repository<PropertyAggregate>()
            .FindAsync(p => propertyIds.Contains(p.Id), cancellationToken);
        var propertiesById = properties.ToDictionary(p => p.Id);

        var ownerIds = page.Items.Select(l => l.OwnerUserId).Distinct().ToList();
        var ownerUsers = await unitOfWork.Repository<UserAggregate>()
            .FindAsync(u => ownerIds.Contains(u.Id), cancellationToken);
        var ownersById = ownerUsers.ToDictionary(u => u.Id, u => u);

        var items = page.Items
            .Select(l =>
            {
                var property = propertiesById.GetValueOrDefault(l.PropertyId);
                var owner = ownersById.GetValueOrDefault(l.OwnerUserId);
                var ownerName = string.IsNullOrWhiteSpace(owner?.FirstName)
                    ? (owner?.Email.Value ?? string.Empty)
                    : string.Join(" ", owner.FirstName, owner.LastName ?? string.Empty).Trim();
                var locationParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(property?.PropertyAddress.Area))
                    locationParts.Add(property.PropertyAddress.Area);
                if (!string.IsNullOrWhiteSpace(property?.PropertyAddress.City))
                    locationParts.Add(property.PropertyAddress.City);
                return new ListingModerationItem(
                    l.Id,
                    l.Title,
                    l.Price.Amount,
                    l.Price.Currency,
                    l.Status,
                    l.OwnerUserId,
                    ownerName,
                    l.PropertyId,
                    string.Join(", ", locationParts),
                    property?.IsVerified == true,
                    l.CreatedAt,
                    l.PublishedAt);
            })
            .ToList();

        return Result<PagedResult<ListingModerationItem>>.Ok(
            new PagedResult<ListingModerationItem>(page.Page, page.PageSize, page.TotalCount, items));
    }
}
