using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Paged lawyer panel for the admin approve/reject/suspend workflow.
/// Optional status filter (e.g. Pending for the verification queue).
/// Admin-only.
/// </summary>
public sealed record GetLawyersQuery(
    LawyerStatus? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<LawyerPanelItem>>>;
