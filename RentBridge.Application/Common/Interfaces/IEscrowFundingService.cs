using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Confirms that a charge identified by <paramref name="reference"/> has been
/// paid, marks the matching escrow payment funded, writes the funding ledger
/// line, and kicks off the automatic payout.
///
/// Idempotent by design: the signed webhook and the browser checkout callback
/// both call this for the same reference, and whichever arrives first funds the
/// escrow — the second becomes a no-op. A reference with no matching escrow
/// payment is acknowledged rather than failed.
/// </summary>
public interface IEscrowFundingService
{
    Task<Result> ConfirmChargeAsync(string reference, CancellationToken cancellationToken);
}
