using MediatR;
using RentBridge.Application.Dtos.PayoutAccount;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.PayoutAccount;

public sealed record GetBanksQuery : IRequest<Result<IReadOnlyList<BankDto>>>;
