using RentBridge.Application.Dtos.Dashboard;

namespace RentBridge.Application.Common.Metrics;

/// <summary>
/// Turns sparse SQL buckets into the continuous series charts need: every
/// bucket in range is present, empty ones as zeros, oldest first. All bucket
/// math is UTC, matching the SQL date_trunc (Monday-start weeks).
/// </summary>
public static class TransactionMetricsSeries
{
    public static IReadOnlyList<TransactionMetricsPoint> FillGaps(
        IReadOnlyList<TransactionMetricsPoint> points,
        DateTimeOffset? from,
        DateTimeOffset? to,
        MetricsGranularity granularity)
    {
        var byPeriod = points.ToDictionary(p => p.Period);

        DateTimeOffset firstBucket;
        DateTimeOffset lastBucket;

        if (byPeriod.Count == 0)
        {
            // No data: still honor an explicitly requested range (all zeros),
            // otherwise there is nothing to show.
            if (!from.HasValue || !to.HasValue)
            {
                return [];
            }
            firstBucket = Truncate(from.Value, granularity);
            lastBucket = LastBucket(to.Value, granularity);
        }
        else
        {
            firstBucket = from.HasValue
                ? Truncate(from.Value, granularity)
                : byPeriod.Keys.Min();
            lastBucket = to.HasValue
                ? LastBucket(to.Value, granularity)
                : byPeriod.Keys.Max();
        }

        if (lastBucket < firstBucket)
        {
            return [];
        }

        var series = new List<TransactionMetricsPoint>();
        for (var bucket = firstBucket; bucket <= lastBucket; bucket = Step(bucket, granularity))
        {
            if (byPeriod.TryGetValue(bucket, out var point))
            {
                series.Add(point);
            }
            else
            {
                series.Add(new TransactionMetricsPoint(bucket, 0, 0, 0, 0, 0));
            }
        }

        return series;
    }

    /// <summary>Last bucket fully before the exclusive upper bound.</summary>
    private static DateTimeOffset LastBucket(DateTimeOffset to, MetricsGranularity granularity)
    {
        var truncated = Truncate(to, granularity);
        return truncated == to ? StepBack(truncated, granularity) : truncated;
    }

    public static DateTimeOffset Truncate(DateTimeOffset value, MetricsGranularity granularity)
    {
        var utc = value.ToUniversalTime();
        return granularity switch
        {
            MetricsGranularity.Month => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
            MetricsGranularity.Week => MondayOf(utc),
            _ => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
        };
    }

    private static DateTimeOffset MondayOf(DateTimeOffset utc)
    {
        // DayOfWeek: Sunday = 0 .. Saturday = 6; offset back to Monday.
        var daysBack = ((int)utc.DayOfWeek + 6) % 7;
        var monday = utc.AddDays(-daysBack);
        return new DateTimeOffset(monday.Year, monday.Month, monday.Day, 0, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset Step(DateTimeOffset bucket, MetricsGranularity granularity) =>
        granularity switch
        {
            MetricsGranularity.Month => bucket.AddMonths(1),
            MetricsGranularity.Week => bucket.AddDays(7),
            _ => bucket.AddDays(1),
        };

    private static DateTimeOffset StepBack(DateTimeOffset bucket, MetricsGranularity granularity) =>
        granularity switch
        {
            MetricsGranularity.Month => bucket.AddMonths(-1),
            MetricsGranularity.Week => bucket.AddDays(-7),
            _ => bucket.AddDays(-1),
        };
}
