using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Command.Payments;

public sealed class HandlePaymentWebhookCommandHandler(
    IUnitOfWork unitOfWork,
    IEscrowProvider escrowProvider,
    IEscrowFundingService fundingService,
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
        var lease = await unitOfWork.Leases.GetForEscrowSettlementByReferenceAsync(
            notification.Reference,
            cancellationToken);        if (lease is null)
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
            _ => await HandleChargeAsync(payment, notification, cancellationToken),
        };
    }

    /// <summary>
    /// Charge events fund escrow. The success path is delegated to the shared
    /// funding service (which the browser callback also uses) so a payment is
    /// recorded whether the webhook or the checkout redirect arrives first; a
    /// failed charge is marked on the payment directly.
    /// </summary>
    private async Task<Result> HandleChargeAsync(
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

        return await fundingService.ConfirmChargeAsync(notification.Reference, cancellationToken);
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

        // Both parties' identity is a user-level fact read at finalize time, not a
        // lease receipt. A missing user row reads as not-verified and fails safe.
        var tenantVerified = await unitOfWork.Repository<UserAggregate>()
            .AnyAsync(u => u.Id == lease.TenantUserId && u.IdentityVerified, cancellationToken);
        var landlordVerified = await unitOfWork.Repository<UserAggregate>()
            .AnyAsync(u => u.Id == lease.LandlordUserId && u.IdentityVerified, cancellationToken);

        var released = lease.Release(tenantVerified, landlordVerified);
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
