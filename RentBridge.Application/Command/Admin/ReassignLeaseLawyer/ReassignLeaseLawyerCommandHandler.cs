using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Command.Admin;

public sealed class ReassignLeaseLawyerCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    ILogger<ReassignLeaseLawyerCommandHandler> logger)
    : IRequestHandler<ReassignLeaseLawyerCommand, Result>
{
    public async Task<Result> Handle(ReassignLeaseLawyerCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (res.IsSuccess is false)
        {
            return Result.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted lease lawyer reassignment without admin role", actor.Id);
            return Result.Forbid("Only an admin can reassign a lease's lawyer.");
        }

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found for reassignment", request.LeaseId);
            return Result.Fail("Lease not found.");
        }

        var lawyer = await unitOfWork.Repository<UserAggregate>()
            .FirstOrDefault(u => u.Id == request.LawyerId, cancellationToken);
        if (lawyer is null)
        {
            return Result.Fail("Lawyer not found.");
        }

        if (lawyer.Role != UserRole.Lawyer)
        {
            return Result.Fail("The selected user is not a lawyer.");
        }

        if (!await lawyerService.IsVerifiedLawyerAsync(request.LawyerId, cancellationToken))
        {
            logger.LogInformation("Cannot reassign lease {LeaseId} to unverified lawyer {LawyerId}", request.LeaseId, request.LawyerId);
            return Result.Fail("The selected lawyer is not verified and cannot be assigned.");
        }

        var previous = lease.AssignedLawyerId;
        var reassign = lease.ReassignLawyer(request.LawyerId);
        if (!reassign.IsSuccess)
        {
            logger.LogInformation("Cannot reassign lawyer for lease {LeaseId}: {Error}", request.LeaseId, reassign.Error);
            return Result.Fail(reassign.Error!);
        }

        unitOfWork.Repository<AuditLog>().Add(
            new AuditLog(
                actor.Id,
                "LeaseLawyerReassigned",
                targetType: "Lease",
                targetId: lease.Id,
                details: $"{{\"from\":\"{previous?.ToString() ?? string.Empty}\",\"to\":\"{request.LawyerId}\"}}"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
