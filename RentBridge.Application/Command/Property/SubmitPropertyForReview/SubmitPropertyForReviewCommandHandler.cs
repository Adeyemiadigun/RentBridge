using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class SubmitPropertyForReviewCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    ILogger<SubmitPropertyForReviewCommandHandler> logger) : IRequestHandler<SubmitPropertyForReviewCommand, Result>
{
    public async Task<Result> Handle(SubmitPropertyForReviewCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        // Only landlord/caretaker/agent can submit their own property for review
        if (user.Role is not (UserRole.Landlord or UserRole.Caretaker or UserRole.Agent))
        {
            logger.LogInformation("User {userId} is not authorized to submit property for review", user.Id);
            return Result.Forbid("Only property owners can submit for review.");
        }

        // User must be identity verified
        if (!user.IdentityVerified)
        {
            return Result.Fail("You must complete identity verification (KYC) before submitting your property for review.");
        }

        var property = await unitOfWork.Repository<PropertyAggregate>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {propertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        // Ensure user owns this property
        if (property.OwnerUserId != user.Id)
        {
            logger.LogInformation("User {userId} attempted to submit property {propertyId} they don't own", user.Id, request.PropertyId);
            return Result.Forbid("You can only submit your own properties for review.");
        }

        // Property must not already be verified
        if (property.IsVerified)
        {
            return Result.Fail("Property is already verified.");
        }

        // Property must have at least one document
        if (!property.Documents.Any())
        {
            return Result.Fail("Property must have at least one ownership document before submitting for review.");
        }

        // Assign a lawyer if not already assigned
        if (property.VerificationLawyerId is null)
        {
            var assignment = await lawyerService.PickNextVerifiedLawyerAsync(cancellationToken);
            if (!assignment.IsSuccess)
            {
                logger.LogWarning("No verified lawyer available for property {PropertyId}", property.Id);
                return Result.Fail("No verified lawyer is currently available. Please try again later or contact support.");
            }

            var assignResult = property.AssignVerificationLawyer(assignment.Value);
            if (!assignResult.IsSuccess)
            {
                return Result.Fail(assignResult.Error!);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Property {PropertyId} submitted for review by owner {UserId}, assigned to lawyer {LawyerId}",
            property.Id, user.Id, property.VerificationLawyerId);

        return Result.Ok();
    }
}