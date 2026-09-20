using MediatR;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Platform-wide operational view: user/listing/lease counts, money currently
/// held in escrow, and transaction metrics summed from the immutable ledger.
/// Admin-only.
/// </summary>
public sealed record GetAdminDashboardQuery : IRequest<Result<AdminDashboardResponse>>;
