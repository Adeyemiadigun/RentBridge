using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Common.Interfaces;

public interface ILawyerAssignmentService
{
    Task<Result<Guid>> PickNextVerifiedLawyerAsync(CancellationToken ct);

    /// <summary>
    /// True only when the user is a lawyer whose profile is currently
    /// <c>LawyerStatus.Verified</c>. Callers use this to re-check an existing
    /// assignment at action time: a lawyer can be suspended or rejected after
    /// being assigned, and that must stop them acting on the assignment.
    /// </summary>
    Task<bool> IsVerifiedLawyerAsync(Guid lawyerId, CancellationToken ct);

    /// <summary>
    /// Resolves the lawyer that should handle the lease for a given listing,
    /// keeping continuity with property verification: if the listing's property
    /// still has a verified verification lawyer, that same lawyer is reused for
    /// the lease legal review; otherwise a fresh round-robin nominee is picked.
    /// </summary>
    Task<Result<Guid>> ResolveLeaseLawyerAsync(Guid listingId, CancellationToken ct);

    /// <summary>
    /// Returns the listing's property verification lawyer when that lawyer is
    /// still verified, otherwise <c>null</c>. Unlike
    /// <see cref="ResolveLeaseLawyerAsync"/> this never falls back to a round-robin
    /// pick, so callers can enforce continuity without overriding an existing
    /// assignment when no property lawyer is available.
    /// </summary>
    Task<Guid?> GetPropertyVerificationLawyerAsync(Guid listingId, CancellationToken ct);

    Task<Result<Guid>> ResolveAndAuthorizeAsync(PropertyAggregate property, User actor, CancellationToken ct);
}