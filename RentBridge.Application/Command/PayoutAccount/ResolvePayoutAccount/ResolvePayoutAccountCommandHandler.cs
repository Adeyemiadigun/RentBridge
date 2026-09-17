using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.PayoutAccount;

public sealed class ResolvePayoutAccountCommandHandler(
    ICurrentUser currentUser,
    IEscrowProvider escrowProvider,
    ILogger<ResolvePayoutAccountCommandHandler> logger)
    : IRequestHandler<ResolvePayoutAccountCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ResolvePayoutAccountCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<string>.Fail(res.Error!);
        }

        var resolved = await escrowProvider.ResolveAccountAsync(
            request.AccountNumber, request.BankCode, cancellationToken);
        if (!resolved.IsSuccess)
        {
            logger.LogInformation("Bank account resolution failed: {Error}", resolved.Error);
            return Result<string>.Fail(resolved.Error!);
        }

        return Result<string>.Ok(resolved.Value.AccountName);
    }
}
