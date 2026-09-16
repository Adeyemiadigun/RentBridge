using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Command.Lease;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Payments;

public sealed class HandlePaymentWebhookCommandHandler(
    IUnitOfWork unitOfWork,
    IEscrowProvider escrowProvider,
    IMediator mediator,
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

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(
                l => l.EscrowPayments.Any(p => p.Reference == notification.Reference),
                cancellationToken);
        if (lease is null)
        {
            logger.LogWarning("Payment webhook for unknown reference {Reference}", notification.Reference);
            return Result.Ok();
        }

        var payment = lease.EscrowPayments.Single(p => p.Reference == notification.Reference);

        if (notification.Status == PaymentStatus.Failed)
        {
            payment.MarkFailed();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Escrow payment {Reference} marked failed", notification.Reference);
            return Result.Ok();
        }

        if (payment.Status == EscrowStatus.Funded)
        {
            logger.LogInformation("Escrow payment {Reference} already processed", notification.Reference);
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

        if (!string.IsNullOrWhiteSpace(lease.LandlordPayoutRecipientCode))
        {
            var release = await mediator.Send(new ReleaseEscrowCommand(lease.Id), cancellationToken);
            if (!release.IsSuccess)
            {
                logger.LogWarning("Automatic release after payment {Reference} failed: {Error}",
                    notification.Reference, release.Error);
            }
        }

        return Result.Ok();
    }
}