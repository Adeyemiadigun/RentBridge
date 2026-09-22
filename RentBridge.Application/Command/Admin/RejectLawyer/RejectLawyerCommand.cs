using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Admin-only: rejects a pending lawyer application. Rejection is terminal —
/// a rejected lawyer cannot be verified afterwards.
/// </summary>
public sealed record RejectLawyerCommand(Guid UserId) : IRequest<Result<Guid>>;
