using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Services;

/// <summary>
/// Automatic escrow payout. Payout is initiated as soon as escrow is funded and
/// identity, inspection, and legal gates have all passed — no landlord action.
/// A failed transfer is retried a bounded number of times, the owner is
/// notified, and an admin can retry/force from the audit-logged endpoint.
/// </summary>
public sealed class EscrowReleaseService(
    IUnitOfWork unitOfWork,
    IEscrowProvider escrowProvider,
    IEmailService emailService,
    IBackgroundJobDispatcher backgroundJobs,
    ILedgerService ledgerService,
    ILogger<EscrowReleaseService> logger) : IEscrowReleaseService
{
    private const int MaxAutoAttempts = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    public async Task<Result> TryAutoReleaseAsync(Guid leaseId, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == leaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Auto-release skipped: lease {LeaseId} not found", leaseId);
            return Result.Fail("Lease not found.");
        }

        if (lease.Status == LeaseStatus.Released)
        {
            return Result.Ok();
        }

        var payment = lease.EscrowPayments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status is EscrowStatus.Funded
                or EscrowStatus.Releasing
                or EscrowStatus.Released
                or EscrowStatus.PayoutFailed);
        if (payment is null || payment.Status == EscrowStatus.Released)
        {
            return Result.Ok();
        }

        // Not releasable until every gate is stamped. If a gate is still open,
        // the trigger that closes it (or a later funding event) will re-attempt.
        if (lease.IdentityGatePassed is null || lease.InspectionGatePassed is null || lease.LegalGatePassed is null)
        {
            return Result.Ok();
        }

        if (payment.Status == EscrowStatus.Releasing)
        {
            // Transfer already in flight — the provider webhook finalizes it.
            return Result.Ok();
        }

        if (string.IsNullOrWhiteSpace(lease.LandlordPayoutRecipientCode))
        {
            logger.LogWarning("Lease {LeaseId} passed all gates but has no payout recipient", leaseId);
            return Result.Fail("Payout recipient is not set for this lease.");
        }

        if (payment.Split is null)
        {
            return Result.Fail("Fee split was not computed for this escrow payment.");
        }

        // Deterministic payout reference: reused across retries so Paystack's own
        // duplicate-reference rejection is a second idempotency guard.
        var payoutReference = string.IsNullOrWhiteSpace(payment.PayoutReference)
            ? $"{payment.Reference}-PAYOUT"
            : payment.PayoutReference!;

        // DB-level claim BEFORE any money moves. Only the winner of the race may transfer.
        var claimed = await unitOfWork.TryClaimEscrowPayoutAsync(payment.Id, payoutReference, cancellationToken);
        if (!claimed)
        {
            logger.LogInformation(
                "Payout for lease {LeaseId} already claimed by another request; skipping transfer", leaseId);
            return Result.Ok();
        }

        // The claim already wrote Releasing to the DB; mirror it on the tracked
        // aggregate (tracking-safe — no re-read that could return a stale instance).
        payment.AttachPayoutReference(payoutReference);
        var releasing = payment.MarkReleasing();
        if (!releasing.IsSuccess)
        {
            return Result.Fail(releasing.Error!);
        }

        // BeginRelease only transitions FundedInEscrow → Releasing; on a retry the
        // lease is already Releasing, so skip it.
        if (lease.Status == LeaseStatus.FundedInEscrow)
        {
            var begin = lease.BeginRelease();
            if (!begin.IsSuccess)
            {
                await RecordPayoutFailureAsync(lease, payment, cancellationToken);
                return Result.Fail(begin.Error!);
            }
        }

        Result<TransferResult> transfer;
        try
        {
            transfer = await escrowProvider.TransferSplitAsync(
                new SplitTransferRequest(
                    payoutReference,
                    payment.GrossAmount.Currency,
                    payment.Split.LandlordPayout.Amount,
                    lease.LandlordPayoutRecipientCode!),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payout transfer threw for lease {LeaseId}; recording failure", leaseId);
            await RecordPayoutFailureAsync(lease, payment, cancellationToken);
            return Result.Fail("Payout transfer failed unexpectedly.");
        }

        if (!transfer.IsSuccess)
        {
            await RecordPayoutFailureAsync(lease, payment, cancellationToken);
            return Result.Fail(transfer.Error!);
        }

        payment.AttachPayoutReference(transfer.Value.ProviderReference);

        // Paystack returns "success" synchronously for balance transfers; when
        // it does we can finalize immediately, otherwise wait for transfer.success.
        if (string.Equals(transfer.Value.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var released = lease.Release();
            if (!released.IsSuccess)
            {
                return Result.Fail(released.Error!);
            }

            var marked = payment.MarkReleased();
            if (!marked.IsSuccess)
            {
                return Result.Fail(marked.Error!);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Payout initiated for lease {LeaseId} (payment {Reference}, status {Status})",
            leaseId, payment.Reference, payment.Status);
        return Result.Ok();
    }

    public Task RetryAutoReleaseAsync(Guid leaseId) => TryAutoReleaseAsync(leaseId, CancellationToken.None);

    public async Task HandlePayoutFailureAsync(Guid leaseId, CancellationToken cancellationToken)
    {
        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == leaseId, cancellationToken);
        if (lease is null)
        {
            return;
        }

        var payment = lease.EscrowPayments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status is EscrowStatus.Releasing
                or EscrowStatus.Funded
                or EscrowStatus.PayoutFailed);
        if (payment is null)
        {
            return;
        }

        await RecordPayoutFailureAsync(lease, payment, cancellationToken);
    }

    public async Task ReconcileStuckPayoutsAsync()
    {
        var cancellationToken = CancellationToken.None;
        var cutoff = DateTimeOffset.UtcNow - RetryDelay;

        // One tracked, server-side query: leases with a payout stuck in Releasing.
        var leases = await unitOfWork.Leases.GetWithStuckPayoutsAsync(cutoff, cancellationToken);

        foreach (var lease in leases)
        {
            var payment = lease.EscrowPayments
                .Where(p => p.Status == EscrowStatus.Releasing
                    && p.PayoutStartedAt != null
                    && p.PayoutStartedAt < cutoff)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();
            if (payment is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(payment.PayoutReference))
            {
                logger.LogWarning("Stuck payout for lease {LeaseId} has no reference; recording failure", lease.Id);
                await RecordPayoutFailureAsync(lease, payment, cancellationToken);
                continue;
            }

            Result<TransferResult> lookup;
            try
            {
                lookup = await escrowProvider.GetTransferStatusAsync(payment.PayoutReference!, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transfer lookup failed while reconciling lease {LeaseId}; retrying next sweep", lease.Id);
                continue;
            }

            if (!lookup.IsSuccess)
            {
                // The provider has no record of the transfer: the process died after
                // the claim but before the transfer was sent. Re-drive via the normal
                // failure/retry path so a retry re-claims and sends it.
                logger.LogWarning("Stuck payout {Reference} not found at provider; re-driving (lease {LeaseId})",
                    payment.PayoutReference, lease.Id);
                await RecordPayoutFailureAsync(lease, payment, cancellationToken);
                continue;
            }

            switch (lookup.Value.Status?.ToLowerInvariant())
            {
                case "success":
                    if (lease.Status == LeaseStatus.FundedInEscrow)
                    {
                        var begin = lease.BeginRelease();
                        if (!begin.IsSuccess)
                        {
                            logger.LogWarning("Cannot begin release while reconciling lease {LeaseId}: {Error}", lease.Id, begin.Error);
                            continue;
                        }
                    }

                    var released = lease.Release();
                    if (!released.IsSuccess)
                    {
                        logger.LogWarning("Cannot release while reconciling lease {LeaseId}: {Error}", lease.Id, released.Error);
                        continue;
                    }

                    payment.MarkReleased();
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Reconciled stuck payout {Reference} as released (lease {LeaseId})",
                        payment.PayoutReference, lease.Id);
                    break;

                case "failed":
                case "reversed":
                case "abandoned":
                    await RecordPayoutFailureAsync(lease, payment, cancellationToken);
                    break;

                default:
                    // pending / processing / otp — still in flight; leave for next sweep.
                    logger.LogInformation("Stuck payout {Reference} still {Status} at provider; re-checking next sweep",
                        payment.PayoutReference, lookup.Value.Status);
                    break;
            }
        }
    }

    private async Task RecordPayoutFailureAsync(
        LeaseAggregate lease,
        EscrowPayment payment,
        CancellationToken cancellationToken)
    {
        payment.IncrementPayoutAttempt();
        var failed = payment.MarkPayoutFailed();
        if (!failed.IsSuccess)
        {
            logger.LogWarning("Could not mark payment {Reference} payout-failed: {Error}", payment.Reference, failed.Error);
        }

        // Ledger line is staged with the state change and committed in the same save.
        await ledgerService.RecordPayoutFailureAsync(lease, payment, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyPayoutFailureAsync(lease, payment, cancellationToken);

        if (payment.Status == EscrowStatus.PayoutFailed && payment.PayoutAttempts < MaxAutoAttempts)
        {
            backgroundJobs.Schedule<IEscrowReleaseService>(
                s => s.RetryAutoReleaseAsync(lease.Id),
                RetryDelay);
            logger.LogInformation("Scheduled automatic payout retry for lease {LeaseId} (attempt {Attempt})",
                lease.Id, payment.PayoutAttempts + 1);
        }
        else
        {
            logger.LogWarning("Automatic payout retries exhausted for lease {LeaseId}; admin retry required", lease.Id);
        }
    }

    private async Task NotifyPayoutFailureAsync(
        LeaseAggregate lease,
        EscrowPayment payment,
        CancellationToken cancellationToken)
    {
        var landlord = await unitOfWork.Repository<UserAggregate>()
            .GetByIdAsync(lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            logger.LogWarning("Payout failure for lease {LeaseId} has unknown landlord {LandlordUserId}",
                lease.Id, lease.LandlordUserId);
            return;
        }

        var willRetry = payment.PayoutAttempts < MaxAutoAttempts;
        var subject = "Escrow payout could not be completed";
        var body =
            $"<h3>Escrow payout could not be completed</h3><p>Hello {landlord.FirstName},</p>" +
            $"<p>The automatic payout for lease <strong>{lease.Id}</strong> failed on attempt {payment.PayoutAttempts}.</p>" +
            (willRetry
                ? "<p>We will retry automatically. If the problem persists, please review your payout bank account.</p>"
                : "<p>Automatic retries are exhausted. Please review your payout bank account, or contact support so an admin can retry the payout.</p>");

        await emailService.SendEmailAsync(landlord.Email.Value, subject, body, cancellationToken);
    }
}
