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
        var verifiedByPropertyId = properties.ToDictionary(p => p.Id, p => p.IsVerified);

        var items = page.Items
            .Select(l => new ListingModerationItem(
                l.Id,
                l.Title,
                l.Price.Amount,
                l.Price.Currency,
                l.Status,
                l.OwnerUserId,
                l.PropertyId,
                verifiedByPropertyId.GetValueOrDefault(l.PropertyId, false),
                l.CreatedAt,
                l.PublishedAt))
            .ToList();

        return Result<PagedResult<ListingModerationItem>>.Ok(
            new PagedResult<ListingModerationItem>(page.Page, page.PageSize, page.TotalCount, items));
    }
}
