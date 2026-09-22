using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Entities;
using System.Linq.Expressions;

namespace RentBridge.Infrastructure.Persistence.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly AppDbContext _context;

    public LedgerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LedgerTypeTotal>> GetTotalsAsync(
        Expression<Func<LedgerEntry, bool>>? predicate,
        CancellationToken ct)
    {
        var query = _context.Set<LedgerEntry>().AsNoTracking().AsQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query
            .GroupBy(e => new { e.Type, e.Currency })
            .Select(g => new LedgerTypeTotal(
                g.Key.Type,
                g.Key.Currency,
                g.Sum(e => e.Amount),
                g.Count()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TransactionMetricsPoint>> GetTransactionMetricsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        MetricsGranularity granularity,
        Guid? landlordUserId,
        CancellationToken ct)
    {
        // Embedded literal: only ever one of three values from the switch, so
        // no user input reaches the SQL text. Bounds stay parameterized.
        var trunc = granularity switch
        {
            MetricsGranularity.Week => "week",
            MetricsGranularity.Month => "month",
            _ => "day",
        };

        // date_trunc runs in UTC explicitly so buckets never shift with the
        // database session's TimeZone. `type` holds the enum names as strings.
        var sql = $"""
            SELECT date_trunc('{trunc}', occurred_at AT TIME ZONE 'UTC') AS "Period",
                   COALESCE(SUM(amount) FILTER (WHERE type = 'EscrowFunded'), 0) AS "Funded",
                   COALESCE(SUM(amount) FILTER (WHERE type = 'LandlordPayout'), 0) AS "PaidOut",
                   COALESCE(SUM(amount) FILTER (WHERE type = 'PlatformCommission'), 0) AS "Commission",
                   COALESCE(SUM(amount) FILTER (WHERE type = 'LegalFeeShare'), 0) AS "LegalFees",
                   COUNT(*) FILTER (WHERE type = 'PayoutFailed') AS "FailedAttempts"
            FROM ledger_entries
            WHERE (@from IS NULL OR occurred_at >= @from)
              AND (@to IS NULL OR occurred_at < @to)
              AND (@owner IS NULL OR landlord_user_id = @owner)
            GROUP BY 1
            ORDER BY 1
            """;

        var parameters = new NpgsqlParameter[]
        {
            new("@from", NpgsqlDbType.TimestampTz) { Value = (object?)from ?? DBNull.Value },
            new("@to", NpgsqlDbType.TimestampTz) { Value = (object?)to ?? DBNull.Value },
            new("@owner", NpgsqlDbType.Uuid) { Value = (object?)landlordUserId ?? DBNull.Value },
        };

        var rows = await _context.Database
            .SqlQueryRaw<TransactionMetricsRow>(sql, parameters)
            .ToListAsync(ct);

        return rows
            .Select(r => new TransactionMetricsPoint(
                new DateTimeOffset(DateTime.SpecifyKind(r.Period, DateTimeKind.Utc)),
                r.Funded,
                r.PaidOut,
                r.Commission,
                r.LegalFees,
                (int)r.FailedAttempts))
            .ToList();
    }
}
