using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Services;

/// <summary>
/// Single writer for "money confirmed received". Both the signed Paystack
/// webhook and the browser checkout callback route through here so a payment is
/// never left unrecorded just because one of the two delivery paths is missing.
/// Funding is guarded by <see cref="EscrowStatus.Initialized"/> so a replayed
/// event cannot fund twice, and the ledger line is staged with the state change
/// and committed in one save.
/// </summary>
public sealed class EscrowFundingService(
    IUnitOfWork unitOfWork,
    IEscrowReleaseService releaseService,
    ILedgerService ledgerService,
    ILogger<EscrowFundingService> logger) : IEscrowFundingService
{
    public async Task<Result> ConfirmChargeAsync(string reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return Result.Fail("Payment reference is required.");
        }

        var lease = await unitOfWork.Leases.GetForEscrowSettlementByReferenceAsync(reference, cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("Charge confirmation for unknown reference {Reference}", reference);
            return Result.Ok();
        }

        var payment = lease.EscrowPayments
            .FirstOrDefault(p => p.Reference == reference || p.PayoutReference == reference);
        if (payment is null)
        {
            logger.LogWarning(
                "Charge confirmation for reference {Reference} but no escrow payment matched", reference);
            return Result.Ok();
        }

        if (payment.Status is not EscrowStatus.Initialized)
        {
            logger.LogInformation(
                "Charge confirmation for {Reference} ignored; payment is {Status}", reference, payment.Status);
            return Result.Ok();
        }

        var funded = payment.MarkFunded();
        if (!funded.IsSuccess)
        {
            logger.LogWarning("Cannot mark payment {Reference} funded: {Error}", reference, funded.Error);
            return Result.Fail(funded.Error!);
        }

        var confirmed = lease.ConfirmEscrowFunding(payment);
        if (!confirmed.IsSuccess)
        {
            logger.LogWarning("Cannot confirm escrow funding for {Reference}: {Error}", reference, confirmed.Error);
            return Result.Fail(confirmed.Error!);
        }

        await ledgerService.RecordFundingAsync(lease, payment, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Escrow payment {Reference} confirmed paid", reference);

        // The single automatic payout trigger: money is now confirmed received.
        // If every gate has already passed the payout runs immediately; otherwise
        // the bounded retry job (RetryAutoReleaseAsync) picks it up.
        var release = await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);
        if (!release.IsSuccess)
        {
            logger.LogWarning("Automatic payout after payment {Reference} did not complete: {Error}",
                reference, release.Error);
        }

        return Result.Ok();
    }
}
