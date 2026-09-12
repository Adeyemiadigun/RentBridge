using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

public class RejectRescheduleCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RejectRescheduleCommandHandler> logger)
    : IRequestHandler<RejectRescheduleCommand, Result>
{
    public async Task<Result> Handle(RejectRescheduleCommand request, CancellationToken cancellationToken)
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

        if (lease.LandlordUserId != user.Id && user.Role != UserRole.Admin)
        {
            logger.LogInformation("User {userId} is not authorized to reject reschedule for lease {leaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the landlord or an admin can reject a reschedule");
        }

        var result = lease.RejectReschedule();
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {leaseId} cannot reject reschedule: {error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}