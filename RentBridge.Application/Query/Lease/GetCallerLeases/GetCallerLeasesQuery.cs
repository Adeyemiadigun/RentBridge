using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Lease;

public sealed record GetCallerLeasesQuery(int Page, int PageSize)
    : IRequest<Result<PagedResult<LeaseListItem>>>;