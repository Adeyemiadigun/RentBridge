using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Listings;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Listing;

public sealed record SearchListingsQuery(
    int Page = 1,
    int PageSize = 20,
    string? State = null,
    string? City = null,
    string? Area = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ListingStatus? Status = null) : IRequest<Result<PagedResult<ListingSearchItem>>>;