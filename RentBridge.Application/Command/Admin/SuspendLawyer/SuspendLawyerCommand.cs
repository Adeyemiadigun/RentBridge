using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Admin-only: suspends a lawyer's profile so they are no longer eligible for
/// auto-assignment. A suspended lawyer can later be re-verified.
/// </summary>
public sealed record SuspendLawyerCommand(Guid UserId) : IRequest<Result<Guid>>;
