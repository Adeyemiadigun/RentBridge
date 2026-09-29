using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public sealed class CompleteInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEscrowReleaseService releaseService,
    ILogger<CompleteInspectionCommandHandler> logger)
    : IRequestHandler<CompleteInspectionCommand, Result<LeaseTransitionResponse>>
{
    public async Task<Result<LeaseTransitionResponse>> Handle(
        CompleteInspectionCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<LeaseTransitionResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Leases
            .GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {leaseId} not found", request.LeaseId);
            return Result<LeaseTransitionResponse>.Fail("Lease not found");
        }

        if (lease.LandlordUserId != user.Id && user.Role != UserRole.Admin)
        {
            logger.LogInformation(
                "User {userId} is not authorized to complete the inspection for lease {leaseId}",
                user.Id, request.LeaseId);
            return Result<LeaseTransitionResponse>.Fail("Only the landlord or an admin can complete an inspection");
        }

        var result = lease.CompleteInspection(request.ActualDate, request.Notes);
        if (!result.IsSuccess)
        {
            logger.LogInformation(
                "Lease {leaseId} cannot complete inspection: {error}", request.LeaseId, result.Error);
            return Result<LeaseTransitionResponse>.Fail(result.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Inspection completed for lease {leaseId} on {actualDate}", request.LeaseId, request.ActualDate);

        // Completing the inspection is the second of three release gates. If
        // escrow is already funded and this was the last one, the payout runs
        // now — this is the trigger point that matters.
        await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);

        return Result<LeaseTransitionResponse>.Ok(
            new LeaseTransitionResponse(lease.Id, lease.Status.ToString()));
    }
}
