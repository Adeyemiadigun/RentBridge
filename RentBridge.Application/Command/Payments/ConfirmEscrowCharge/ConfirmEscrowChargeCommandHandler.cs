using MediatR;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Payments;

public sealed class ConfirmEscrowChargeCommandHandler(
    IEscrowFundingService fundingService)
    : IRequestHandler<ConfirmEscrowChargeCommand, Result>
{
    public Task<Result> Handle(ConfirmEscrowChargeCommand request, CancellationToken cancellationToken)
        => fundingService.ConfirmChargeAsync(request.Reference, cancellationToken);
}
