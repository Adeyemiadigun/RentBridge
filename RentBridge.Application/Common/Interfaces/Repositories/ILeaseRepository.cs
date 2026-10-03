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

    /// <summary>
    /// Gets a lease by ID with InspectionRequests collection loaded for proper
    /// concurrency tracking of owned entities.
    /// </summary>
    Task<Lease?> GetWithInspectionRequestsAsync(
        Guid leaseId,
        CancellationToken ct);

    /// <summary>
    /// Gets a tracked lease with the Agreement.Signatures and EscrowPayments
    /// collections loaded. Escrow settlement decisions read Agreement.IsFullySigned
    /// and the payment's status, and EF leaves owned collections empty unless they
    /// are explicitly loaded, so the payment path must use this loader.
    /// </summary>
    Task<Lease?> GetWithAgreementGraphAsync(
        Guid leaseId,
        CancellationToken ct);

    /// <summary>
    /// Resolves a lease from an escrow payment reference (funding or payout) with the
    /// Agreement.Signatures and EscrowPayments collections loaded.
    /// </summary>
    Task<Lease?> GetForEscrowSettlementByReferenceAsync(
        string reference,
        CancellationToken ct);
}
