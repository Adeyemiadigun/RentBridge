using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Payments;

/// <summary>
/// Confirms a charge the provider has independently verified — used by the
/// browser checkout callback after it calls Paystack's verify endpoint. The
/// callback is not HMAC-signed, so the caller must have confirmed the charge is
/// actually paid before sending this. Idempotent with the signed webhook:
/// whichever path arrives first funds the escrow, the other is a no-op.
/// </summary>
public sealed record ConfirmEscrowChargeCommand(string Reference) : IRequest<Result>;
