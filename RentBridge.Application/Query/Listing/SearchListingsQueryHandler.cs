using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Listing;

public class SearchListingsQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<SearchListingsQueryHandler> logger) : IRequestHandler<SearchListingsQuery, Result<PagedResult<ListingSearchItem>>>
{
    public async Task<Result<PagedResult<ListingSearchItem>>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.Listings.SearchAsync(
            request.State,
            request.City,
            request.Area,
            request.MinPrice,
            request.MaxPrice,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<ListingSearchItem>>.Ok(result);
    }
}