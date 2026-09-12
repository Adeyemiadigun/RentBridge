using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class AssignVerificationLawyerCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AssignVerificationLawyerCommandHandler> logger) : IRequestHandler<AssignVerificationLawyerCommand, Result>
{
    public async Task<Result> Handle(AssignVerificationLawyerCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role != UserRole.Admin)
        {
            logger.LogInformation("User {userId} is not authorized to assign a verification lawyer", user.Id);
            return Result.Fail("Only an admin can assign a verification lawyer.");
        }

        var property = await unitOfWork.Repository<PropertyAggregate>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {propertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        if (property.VerificationLawyerId != request.LawyerId && property.VerificationLawyerId is not null)
        {
            logger.LogWarning(
                "Admin {adminId} reassigning verification lawyer for property {propertyId}: {previous} -> {next}",
                user.Id, property.Id, property.VerificationLawyerId, request.LawyerId);
        }

        var assignResult = property.AssignVerificationLawyer(request.LawyerId);
        if (!assignResult.IsSuccess)
        {
            logger.LogInformation("Property {propertyId} cannot be assigned lawyer {lawyerId}: {error}", request.PropertyId, request.LawyerId, assignResult.Error);
            return Result.Fail(assignResult.Error!);
        }

        var lawyer = await unitOfWork.Repository<User>().GetByIdAsync(request.LawyerId, cancellationToken);
        lawyer?.LawyerProfile?.MarkAssigned();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}