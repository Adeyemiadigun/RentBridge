using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Common.Interfaces;

public interface ILawyerAssignmentService
{
    Task<Result<Guid>> PickNextVerifiedLawyerAsync(CancellationToken ct);

    Task<Result<Guid>> ResolveAndAuthorizeAsync(PropertyAggregate property, User actor, CancellationToken ct);
}