using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Admin-only fallback: forces or retries the automatic escrow payout for a
/// lease whose payout has failed (or is stuck). The three verification gates
/// are still enforced — there is no bypass. Privileged and audit-logged.
/// Idempotent — a released lease is a no-op.
/// </summary>
public sealed record ReleaseEscrowCommand(Guid LeaseId) : IRequest<Result>;
