using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Payments;

public sealed class HandlePaymentWebhookCommandHandler(
    IUnitOfWork unitOfWork,
    IEscrowProvider escrowProvider,
    IEscrowReleaseService releaseService,
    ILogger<HandlePaymentWebhookCommandHandler> logger)
    : IRequestHandler<HandlePaymentWebhookCommand, Result>
{
    public async Task<Result> Handle(HandlePaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var verified = await escrowProvider.VerifyAndParseAsync(request.RawBody, request.Signature, cancellationToken);
        if (!verified.IsSuccess)
        {
            logger.LogWarning("Payment webhook rejected: {Error}", verified.Error);
            return Result.Fail(verified.Error!);
        }
        var notification = verified.Value;

        // Charge events carry the charge reference; transfer events carry the payout
        // reference (which is derived from it), so match either.
        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(
                l => l.EscrowPayments.Any(p =>
                    p.Reference == notification.Reference || p.PayoutReference == notification.Reference),
                cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("Payment webhook for unknown reference {Reference}", notification.Reference);
            return Result.Ok();
        }

        var payment = lease.EscrowPayments.First(p =>
            p.Reference == notification.Reference || p.PayoutReference == notification.Reference);

        return notification.Kind switch
        {
            PaymentEventKind.TransferSuccess => await HandleTransferSuccessAsync(lease, payment, cancellationToken),
            PaymentEventKind.TransferFailed or PaymentEventKind.TransferReversed
                => await HandleTransferFailureAsync(lease, payment, notification.Kind, cancellationToken),
            _ => await HandleChargeAsync(lease, payment, notification, cancellationToken),
        };
    }

    /// <summary>Charge events fund escrow and kick off the automatic payout.</summary>
    private async Task<Result> HandleChargeAsync(
        LeaseAggregate lease,
        EscrowPayment payment,
        PaymentNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.Status == PaymentStatus.Failed)
        {
            payment.MarkFailed();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Escrow payment {Reference} marked failed", notification.Reference);
            return Result.Ok();
        }

        if (payment.Status is not EscrowStatus.Initialized)
        {
            logger.LogInformation("Charge webhook for {Reference} ignored; payment is {Status}",
                notification.Reference, payment.Status);
            return Result.Ok();
        }

        var funded = payment.MarkFunded();
        if (!funded.IsSuccess)
        {
            logger.LogWarning("Cannot mark payment {Reference} funded: {Error}", notification.Reference, funded.Error);
            return Result.Fail(funded.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Escrow payment {Reference} confirmed paid", notification.Reference);

        // Automatic payout: pays out now if every gate has already passed,
        // otherwise a later gate/funding trigger completes it.
        var release = await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);
        if (!release.IsSuccess)
        {
            logger.LogWarning("Automatic payout after payment {Reference} did not complete: {Error}",
                notification.Reference, release.Error);
        }

        return Result.Ok();
    }

    /// <summary>The provider confirmed the payout transferred — finalize.</summary>
    private async Task<Result> HandleTransferSuccessAsync(
        LeaseAggregate lease,
        EscrowPayment payment,
        CancellationToken cancellationToken)
    {
        if (payment.Status == EscrowStatus.Released)
        {
            return Result.Ok();
        }

        if (payment.Status != EscrowStatus.Releasing)
        {
            logger.LogWarning("transfer.success for {Reference} but payment is {Status}",
                payment.Reference, payment.Status);
            return Result.Ok();
        }

        var released = lease.Release();
        if (!released.IsSuccess)
        {
            logger.LogWarning("Cannot release lease {LeaseId}: {Error}", lease.Id, released.Error);
            return Result.Fail(released.Error!);
        }

        var marked = payment.MarkReleased();
        if (!marked.IsSuccess)
        {
            return Result.Fail(marked.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Payout confirmed for lease {LeaseId} (payment {Reference})", lease.Id, payment.Reference);
        return Result.Ok();
    }

    /// <summary>The payout failed — notify the owner and schedule a retry.</summary>
    private async Task<Result> HandleTransferFailureAsync(
        LeaseAggregate lease,
        EscrowPayment payment,
        PaymentEventKind kind,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("{Kind} for payment {Reference} on lease {LeaseId}", kind, payment.Reference, lease.Id);
        await releaseService.HandlePayoutFailureAsync(lease.Id, cancellationToken);
        return Result.Ok();
    }
}
