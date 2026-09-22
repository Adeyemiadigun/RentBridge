using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Command.Admin;

public sealed class SuspendLawyerCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SuspendLawyerCommandHandler> logger)
    : IRequestHandler<SuspendLawyerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SuspendLawyerCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<Guid>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to suspend lawyers without admin role", actor.Id);
            return Result<Guid>.Fail("Only an admin can suspend lawyers.");
        }

        var lawyer = await unitOfWork.Repository<UserAggregate>()
            .FirstOrDefault(u => u.Id == request.UserId, cancellationToken);
        if (lawyer is null)
        {
            logger.LogInformation("User {UserId} not found", request.UserId);
            return Result<Guid>.Fail("User not found.");
        }

        if (lawyer.Role != UserRole.Lawyer)
        {
            return Result<Guid>.Fail("Only lawyer accounts can be suspended.");
        }

        if (lawyer.LawyerProfile is null)
        {
            return Result<Guid>.Fail("Lawyer profile not found for this user.");
        }

        var suspend = lawyer.LawyerProfile.Suspend();
        if (!suspend.IsSuccess)
        {
            logger.LogInformation("Cannot suspend lawyer {UserId}: {Error}", request.UserId, suspend.Error);
            return Result<Guid>.Fail(suspend.Error!);
        }

        unitOfWork.Repository<AuditLog>().Add(
            new AuditLog(actor.Id, "LawyerSuspended", targetType: "User", targetId: lawyer.Id,
                details: $"{{\"barNumber\":\"{lawyer.LawyerProfile.BarNumber}\"}}"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(lawyer.Id);
    }
}
