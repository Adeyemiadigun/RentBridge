using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Landlord-only: stores the Paystack recipient code the escrow payout is
/// sent to. Idempotent; a no-op once the lease has started releasing.
/// </summary>
public sealed record SetPayoutRecipientCommand(Guid LeaseId, string RecipientCode) : IRequest<Result>;