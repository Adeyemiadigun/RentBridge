using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// The lease id plus its status AFTER the transition, so the client never has
/// to guess. A confirmation can trigger further movement via domain events
/// (a lawyer being assigned), so echoing the real status matters.
/// </summary>
public sealed record LeaseTransitionResponse(Guid LeaseId, string Status);

public record class ConfirmInspectionCommand(
    Guid LeaseId,
    DateTimeOffset ScheduledDate,
    string? Notes = null)
    : IRequest<Result<LeaseTransitionResponse>>;
