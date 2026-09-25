using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class VerifyPropertyCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    ILogger<VerifyPropertyCommandHandler> logger) : IRequestHandler<VerifyPropertyCommand, Result>
{
    public async Task<Result> Handle(VerifyPropertyCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {userId} is not authorized to verify a property", user.Id);
            return Result.Fail("Only a lawyer or admin can verify a property.");
        }

        var property = await unitOfWork.Repository<PropertyAggregate>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {propertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        var auth = await lawyerService.ResolveAndAuthorizeAsync(property, user, cancellationToken);
        if (!auth.IsSuccess)
        {
            logger.LogInformation("User {userId} is not authorized to verify property {propertyId}: {error}", user.Id, request.PropertyId, auth.Error);
            return Result.Fail(auth.Error!);
        }

        var verifyResult = property.MarkOwnershipVerified(
            user.Id.ToString(),
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Role.ToString());
        if (!verifyResult.IsSuccess)
        {
            logger.LogInformation("Property {propertyId} cannot be verified: {error}", request.PropertyId, verifyResult.Error);
            return Result.Fail(verifyResult.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}