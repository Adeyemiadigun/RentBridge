using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
namespace RentBridge.Application.Command.Lease;

public class BeginInspectionFlowCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<BeginInspectionFlowCommandHandler> logger)
    : IRequestHandler<BeginInspectionFlowCommand, Result>
{
    public async Task<Result> Handle(BeginInspectionFlowCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Leases.GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            return Result.Fail("Lease not found");
        }

        if (lease.LandlordUserId != user.Id)
        {
            return Result.Forbid("Only the landlord can begin the inspection flow");
        }

        var result = lease.BeginInspectionFlow();
        if (!result.IsSuccess)
        {
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
