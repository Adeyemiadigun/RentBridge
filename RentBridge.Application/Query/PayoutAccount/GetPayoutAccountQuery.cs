using MediatR;
using RentBridge.Application.Dtos.PayoutAccount;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.PayoutAccount;

public sealed record GetPayoutAccountQuery : IRequest<Result<PayoutAccountDto?>>;
