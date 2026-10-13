using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Admin payouts console: paged escrow payouts, newest first, each with its
/// current status, attempt count, last failure reason, and whether it can be
/// retried. Optional status filter. Admin-only.
/// </summary>
public sealed record GetAdminPayoutsQuery(
    EscrowStatus? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<AdminPayoutItem>>>;
