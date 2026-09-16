using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Releases funded escrow to the landlord (net of platform commission and the
/// lawyer's legal-fee share). Callable by the landlord or an admin; the
/// landlord recipient code may be supplied here or stored earlier on the lease.
/// Idempotent — a released lease is a no-op.
/// </summary>
public sealed record ReleaseEscrowCommand(Guid LeaseId, string? RecipientCode = null) : IRequest<Result>;