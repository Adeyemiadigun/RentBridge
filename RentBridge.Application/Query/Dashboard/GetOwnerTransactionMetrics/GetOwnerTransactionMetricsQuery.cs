using MediatR;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Dashboard;

/// <summary>
/// Transaction time-series for the signed-in owner's leases, for charts:
/// per-bucket funded, paid out, commission, legal fees, and failed payout
/// attempts. Landlord, agent, or caretaker only. The `to` bound is exclusive.
/// </summary>
public sealed record GetOwnerTransactionMetricsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    MetricsGranularity Granularity = MetricsGranularity.Day)
    : IRequest<Result<TransactionMetricsResponse>>;
