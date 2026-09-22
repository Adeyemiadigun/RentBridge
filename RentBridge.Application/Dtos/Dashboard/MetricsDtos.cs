namespace RentBridge.Application.Dtos.Dashboard;

/// <summary>Bucket size for transaction time-series metrics.</summary>
public enum MetricsGranularity
{
    Day,
    Week,
    Month,
}

/// <summary>One bucket of the transaction time-series. Amounts are in the
/// ledger lines' own currencies (the platform settles in a single currency).</summary>
public sealed record TransactionMetricsPoint(
    DateTimeOffset Period,
    decimal Funded,
    decimal PaidOut,
    decimal Commission,
    decimal LegalFees,
    int FailedAttempts);

/// <summary>Continuous (gap-filled) transaction time-series, oldest first.
/// Buckets are UTC calendar days / Monday-start weeks / calendar months.
/// The `to` bound is exclusive.</summary>
public sealed record TransactionMetricsResponse(
    DateTimeOffset? From,
    DateTimeOffset? To,
    MetricsGranularity Granularity,
    IReadOnlyList<TransactionMetricsPoint> Points);
