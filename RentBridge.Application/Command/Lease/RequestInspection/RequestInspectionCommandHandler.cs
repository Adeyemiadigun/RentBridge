using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public class RequestInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RequestInspectionCommandHandler> logger)
    : IRequestHandler<RequestInspectionCommand, Result>
{
    public async Task<Result> Handle(RequestInspectionCommand request, CancellationToken cancellationToken)
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

        if (lease.TenantUserId != user.Id)
        {
            logger.LogInformation("User {userId} is not the tenant of lease {leaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the tenant on this lease can request an inspection");
        }

        var result = lease.RequestInspection(user.Id, request.PreferredDate, request.Note);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {leaseId} cannot accept an inspection request: {error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}