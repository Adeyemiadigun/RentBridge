using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Listing;

public sealed class GetListingDetailQueryHandler(
    IUnitOfWork unitOfWork,
    ILogger<GetListingDetailQueryHandler> logger)
    : IRequestHandler<GetListingDetailQuery, Result<ListingDetailItem>>
{
    public async Task<Result<ListingDetailItem>> Handle(
        GetListingDetailQuery request,
        CancellationToken cancellationToken)
    {
        var item = await unitOfWork.Listings.GetDetailAsync(request.ListingId, cancellationToken);
        if (item is null)
        {
            return Result<ListingDetailItem>.Fail("Listing not found.");
        }
        return Result<ListingDetailItem>.Ok(item);
    }
}