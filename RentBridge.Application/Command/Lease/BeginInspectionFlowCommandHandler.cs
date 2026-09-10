using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;

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

        var lease = await unitOfWork.Repository<Lease>().GetByIdAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {leaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        if (lease.LandlordUserId != user.Id)
        {
            logger.LogInformation("User {userId} is not the landlord of lease {leaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the landlord can begin the inspection flow");
        }

        var result = lease.BeginInspectionFlow();
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {leaseId} cannot begin inspection: {error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}