using MediatR;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Dashboard;

/// <summary>
/// Operational view for a listing owner (landlord, agent, or caretaker): their
/// listings and leases by status, money held in escrow on their leases, payout
/// totals from the ledger, and the 5 most recent ledger lines.
/// </summary>
public sealed record GetOwnerDashboardQuery : IRequest<Result<OwnerDashboardResponse>>;
