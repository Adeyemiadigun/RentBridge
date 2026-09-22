using MediatR;
using RentBridge.Application.Common.Payments;
using RentBridge.Application.Dtos.PayoutAccount;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.PayoutAccount;

public sealed class GetBanksQueryHandler(IEscrowProvider escrowProvider)
    : IRequestHandler<GetBanksQuery, Result<IReadOnlyList<BankDto>>>
{
    public async Task<Result<IReadOnlyList<BankDto>>> Handle(GetBanksQuery request, CancellationToken cancellationToken)
    {
        var banks = await escrowProvider.ListBanksAsync("nigeria", "NGN", cancellationToken);
        if (!banks.IsSuccess)
        {
            return Result<IReadOnlyList<BankDto>>.Fail(banks.Error!);
        }

        IReadOnlyList<BankDto> dtos = banks.Value
            .Select(b => new BankDto(b.Code, b.Name, b.Slug))
            .ToList();

        return Result<IReadOnlyList<BankDto>>.Ok(dtos);
    }
}
