using RentBridge.Domain.Aggregates;

namespace RentBridge.Application.Common.Interfaces.Repositories;

public interface ILeaseRepository
{
    /// <summary>
    /// Returns tracked leases that have an escrow payout stuck in Releasing since
    /// before <paramref name="cutoff"/>. The filter runs server-side and the
    /// entities are tracked so callers can finalize the payout and save.
    /// </summary>
    Task<IReadOnlyList<Lease>> GetWithStuckPayoutsAsync(
        DateTimeOffset cutoff,
        CancellationToken ct);
}
