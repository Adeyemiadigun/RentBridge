using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public sealed class ReleaseEscrowCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowProvider escrowProvider,
    ILogger<ReleaseEscrowCommandHandler> logger)
    : IRequestHandler<ReleaseEscrowCommand, Result>
{
    public async Task<Result> Handle(ReleaseEscrowCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var actor = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found.");
        }

        var isLandlord = actor.Id == lease.LandlordUserId;
        var isAdmin = actor.Role is UserRole.Admin;
        if (!isLandlord && !isAdmin)
        {
            logger.LogInformation("User {UserId} tried to release escrow on lease {LeaseId} without authority", actor.Id, request.LeaseId);
            return Result.Fail("Only the landlord or an admin can release escrow.");
        }

        if (lease.Status == LeaseStatus.Released)
        {
            return Result.Ok();
        }

        var payment = lease.EscrowPayments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status is EscrowStatus.Funded or EscrowStatus.Releasing or EscrowStatus.Released);
        if (payment is null || payment.Status == EscrowStatus.Released)
        {
            return Result.Ok();
        }

        if (lease.Status == LeaseStatus.FundedInEscrow)
        {
            if (!string.IsNullOrWhiteSpace(request.RecipientCode))
            {
                var setRecipient = lease.SetLandlordPayoutRecipientCode(request.RecipientCode);
                if (!setRecipient.IsSuccess)
                {
                    return Result.Fail(setRecipient.Error!);
                }
            }

            if (string.IsNullOrWhiteSpace(lease.LandlordPayoutRecipientCode))
            {
                return Result.Fail("Landlord payout recipient is not set. Store it via the payout-recipient endpoint or pass RecipientCode.");
            }

            if (payment.Split is null)
            {
                return Result.Fail("Fee split was not computed for this escrow payment.");
            }

            var transfer = await escrowProvider.TransferSplitAsync(
                new SplitTransferRequest(
                    payment.Reference,
                    payment.GrossAmount.Currency,
                    payment.Split.LandlordPayout.Amount,
                    lease.LandlordPayoutRecipientCode),
                cancellationToken);
            if (!transfer.IsSuccess)
            {
                logger.LogWarning("Transfer failed for payment {PaymentRef}: {Error}", payment.Reference, transfer.Error);
                return Result.Fail(transfer.Error!);
            }

            payment.AttachPayoutReference(transfer.Value.ProviderReference);

            if (lease.Status != LeaseStatus.Releasing)
            {
                // mark both sides "releasing" before finalizing
                var begin = lease.BeginRelease();
                if (!begin.IsSuccess) return Result.Fail(begin.Error!);
                var mark = payment.MarkReleasing();
                if (!mark.IsSuccess) return Result.Fail(mark.Error!);
            }
        }
        else if (lease.Status != LeaseStatus.Releasing)
        {
            return Result.Fail("Escrow is not in a releasable state.");
        }

        var release = lease.Release();
        if (!release.IsSuccess)
        {
            return Result.Fail(release.Error!);
        }

        var markReleased = payment.MarkReleased();
        if (!markReleased.IsSuccess)
        {
            return Result.Fail(markReleased.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}