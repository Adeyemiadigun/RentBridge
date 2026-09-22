namespace RentBridge.Infrastructure.Persistence.Repositories;

/// <summary>
/// Keyless result shape for the ledger time-series query (registered with
/// HasNoKey in AppDbContext). Property names must match the SQL aliases.
/// </summary>
public sealed class TransactionMetricsRow
{
    public DateTime Period { get; set; }
    public decimal Funded { get; set; }
    public decimal PaidOut { get; set; }
    public decimal Commission { get; set; }
    public decimal LegalFees { get; set; }
    public long FailedAttempts { get; set; }
}
