using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Admin-only, explicit reassignment of a lease's assigned lawyer. The lawyer
/// assigned when the property was created owns the matter for its entire life;
/// automatic flows never reassign, so this is the only sanctioned way to move a
/// lease to a different (verified) lawyer — e.g. when the assigned lawyer is no
/// longer verified.
/// </summary>
public sealed record ReassignLeaseLawyerCommand(Guid LeaseId, Guid LawyerId) : IRequest<Result>;
