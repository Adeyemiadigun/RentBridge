using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Listing;

public sealed record GetListingDetailQuery(Guid ListingId) : IRequest<Result<ListingDetailItem>>;