using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;
using PayoutAccountEntity = RentBridge.Domain.Entities.PayoutAccount;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Command.PayoutAccount;

public sealed class SetPayoutAccountCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowProvider escrowProvider,
    ILogger<SetPayoutAccountCommandHandler> logger)
    : IRequestHandler<SetPayoutAccountCommand, Result>
{
    public async Task<Result> Handle(SetPayoutAccountCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        // Authoritative name enquiry: never trust a client-supplied account name.
        var resolved = await escrowProvider.ResolveAccountAsync(
            request.AccountNumber, request.BankCode, cancellationToken);
        if (!resolved.IsSuccess)
        {
            logger.LogInformation("Payout account resolution failed for user {UserId}: {Error}", user.Id, resolved.Error);
            return Result.Fail(resolved.Error!);
        }

        var recipient = await escrowProvider.CreateTransferRecipientAsync(
            new CreateRecipientRequest(
                resolved.Value.AccountName,
                request.AccountNumber,
                request.BankCode,
                "NGN"),
            cancellationToken);
        if (!recipient.IsSuccess)
        {
            logger.LogWarning("Transfer recipient creation failed for user {UserId}: {Error}", user.Id, recipient.Error);
            return Result.Fail(recipient.Error!);
        }

        var accountNumber = request.AccountNumber.Trim();
        var last4 = accountNumber.Length >= 4 ? accountNumber[^4..] : accountNumber;

        var created = PayoutAccountEntity.Create(
            "paystack",
            recipient.Value.RecipientCode,
            request.BankCode,
            request.BankName,
            last4,
            resolved.Value.AccountName,
            DateTimeOffset.UtcNow);
        if (!created.IsSuccess)
        {
            return Result.Fail(created.Error!);
        }

        var set = user.SetPayoutAccount(created.Value);
        if (!set.IsSuccess)
        {
            return Result.Fail(set.Error!);
        }

        unitOfWork.Repository<UserAggregate>().Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Payout account registered for user {UserId} ({Bank}, ****{Last4})",
            user.Id, request.BankName, last4);
        return Result.Ok();
    }
}
