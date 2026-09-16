using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
namespace RentBridge.Application.Command.Lease;

public sealed class BeginLegalReviewCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    ILogger<BeginLegalReviewCommandHandler> logger)
    : IRequestHandler<BeginLegalReviewCommand, Result>
{
    public async Task<Result> Handle(BeginLegalReviewCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        var isParty = user.Id == lease.LandlordUserId || user.Id == lease.TenantUserId;
        var isStaff = user.Role is UserRole.Lawyer or UserRole.Admin;
        if (!isParty && !isStaff)
        {
            logger.LogInformation("User {UserId} is not a party to lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the tenant, landlord, lawyer, or admin can begin legal review.");
        }

        if (lease.AssignedLawyerId is null)
        {
            var picked = await lawyerService.PickNextVerifiedLawyerAsync(cancellationToken);
            if (!picked.IsSuccess)
            {
                logger.LogWarning("No verified lawyer available for lease {LeaseId}: {Error}", request.LeaseId, picked.Error);
                return Result.Fail("No verified lawyer is currently available. Contact an admin.");
            }

            var assign = lease.AssignLawyer(picked.Value);
            if (!assign.IsSuccess)
            {
                logger.LogInformation(
                    "Lease {LeaseId} cannot be assigned lawyer {LawyerId}: {Error}",
                    request.LeaseId, picked.Value, assign.Error);
                return Result.Fail(assign.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }

        var begin = lease.BeginLegalReview();
        if (!begin.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot begin legal review: {Error}", request.LeaseId, begin.Error);
            return Result.Fail(begin.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}