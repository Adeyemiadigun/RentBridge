using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class ConfirmInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowReleaseService releaseService,
    ILogger<ConfirmInspectionCommandHandler> logger)
    : IRequestHandler<ConfirmInspectionCommand, Result>
{
    public async Task<Result> Handle(ConfirmInspectionCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>().GetByIdAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {leaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        if (lease.LandlordUserId != user.Id && user.Role != UserRole.Admin)
        {
            logger.LogInformation("User {userId} is not authorized to confirm inspection for lease {leaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the landlord or an admin can confirm an inspection");
        }

        var result = lease.ConfirmInspection(request.ScheduledDate, request.Notes);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {leaseId} cannot confirm inspection: {error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Inspection is one of the three release gates; if escrow is already
        // funded and this was the last gate, the payout runs now.
        await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);

        return Result.Ok();
    }
}