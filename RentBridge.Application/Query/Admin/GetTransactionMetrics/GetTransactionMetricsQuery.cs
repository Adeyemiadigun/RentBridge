using MediatR;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Platform-wide transaction time-series for charts: per-bucket funded, paid
/// out, commission, legal fees, and failed payout attempts. Admin-only.
/// The `to` bound is exclusive.
/// </summary>
public sealed record GetTransactionMetricsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    MetricsGranularity Granularity = MetricsGranularity.Day)
    : IRequest<Result<TransactionMetricsResponse>>;
