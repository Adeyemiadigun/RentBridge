using MediatR;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.PayoutAccount;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.PayoutAccount;

public sealed class GetPayoutAccountQueryHandler(ICurrentUser currentUser)
    : IRequestHandler<GetPayoutAccountQuery, Result<PayoutAccountDto?>>
{
    public async Task<Result<PayoutAccountDto?>> Handle(GetPayoutAccountQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PayoutAccountDto?>.Fail(res.Error!);
        }

        var account = res.Value.PayoutAccount;
        if (account is null)
        {
            return Result<PayoutAccountDto?>.Ok(null);
        }

        return Result<PayoutAccountDto?>.Ok(new PayoutAccountDto(
            account.Provider,
            account.BankName,
            account.BankCode,
            account.AccountNumberLast4,
            account.AccountName,
            account.IsActive,
            account.VerifiedAt));
    }
}
