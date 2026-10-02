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

    Task<Result<Guid>> ResolveAndAuthorizeAsync(PropertyAggregate property, User actor, CancellationToken ct);
}