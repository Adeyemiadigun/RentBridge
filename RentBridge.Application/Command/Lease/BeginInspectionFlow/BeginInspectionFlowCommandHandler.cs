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
        logger.LogInformation("[DEBUG] BeginInspectionFlowCommand received for LeaseId: {leaseId}", request.LeaseId);
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            logger.LogWarning("[DEBUG] GetCurrentUser failed: {error}", res.Error);
            return Result.Fail(res.Error!);
        }
        var user = res.Value;
        logger.LogInformation("[DEBUG] Current user: {userId}, Role: {role}", user.Id, user.Role);

        var lease = await unitOfWork.Leases.GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("[DEBUG] Lease {leaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        logger.LogInformation("[DEBUG] Lease loaded: Id={id}, Status={status}, LandlordUserId={landlordId}, InspectionRequestsCount={count}", 
            lease.Id, lease.Status, lease.LandlordUserId, lease.InspectionRequests.Count);

        if (lease.LandlordUserId != user.Id)
        {
            logger.LogInformation("[DEBUG] User {userId} is not the landlord of lease {leaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the landlord can begin the inspection flow");
        }

        var result = lease.BeginInspectionFlow();
        if (!result.IsSuccess)
        {
            logger.LogInformation("[DEBUG] Lease {leaseId} cannot begin inspection: {error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[DEBUG] BeginInspectionFlow succeeded");
        return Result.Ok();
    }
}