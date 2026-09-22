using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Paged listing moderation queue: every listing with its owner's id and the
/// property's verification state. Optional status filter. Admin-only.
/// </summary>
public sealed record GetListingsForModerationQuery(
    ListingStatus? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<ListingModerationItem>>>;
