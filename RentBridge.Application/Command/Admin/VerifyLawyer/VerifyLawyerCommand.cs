using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Admin-only: verifies a lawyer's profile so they become eligible for
/// auto-assignment to property reviews and lease legal work.
/// </summary>
public sealed record VerifyLawyerCommand(Guid UserId) : IRequest<Result<Guid>>;