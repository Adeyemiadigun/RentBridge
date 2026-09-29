using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Listing;

public class SearchListingsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SearchListingsQueryHandler> logger) : IRequestHandler<SearchListingsQuery, Result<PagedResult<ListingSearchItem>>>
{
    public async Task<Result<PagedResult<ListingSearchItem>>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        Guid? ownerUserId = null;
        if (request.Mine)
        {
            var callerId = currentUser.UserId;
            if (callerId is null)
            {
                return Result<PagedResult<ListingSearchItem>>.Ok(
                    new PagedResult<ListingSearchItem>(request.Page, request.PageSize, 0, Array.Empty<ListingSearchItem>()));
            }
            ownerUserId = callerId;
        }

        var result = await unitOfWork.Listings.SearchAsync(
            request.State,
            request.City,
            request.Area,
            request.MinPrice,
            request.MaxPrice,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken,
            ownerUserId);

        return Result<PagedResult<ListingSearchItem>>.Ok(result);
    }
}