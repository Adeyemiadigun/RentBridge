using MediatR;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Tenant-only: creates the escrow payment for a fully-signed lease, computes
/// the fee split from current platform settings, and returns the provider's
/// checkout URL. Idempotent per lease (re-returns the existing checkout).
/// </summary>
public sealed record FundEscrowCommand(Guid LeaseId) : IRequest<Result<FundEscrowResponse>>;