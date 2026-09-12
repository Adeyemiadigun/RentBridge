using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property
{
    public class CreatePropertyCommandHandler(IUnitOfWork unitOfWork,ICurrentUser _currentUser,ILawyerAssignmentService lawyerAssignmentService,ILogger<CreatePropertyCommandHandler> logger) : IRequestHandler<CreatePropertyCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
        {
            var res = await _currentUser.GetCurrentUser(true, cancellationToken);
            if (!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }

            var user = res.Value;

            if (!PropertyAggregate.CanCreateBy(user.Role))
            {
                logger.LogInformation("User is not allowed to create property {UserId}", user.Id);
                return Result<Guid>.Fail("User is not allowed to create property");
            }

            var property = new PropertyAggregate(user.Id, request.Street, request.City, request.Area, request.State);

            if (request.DocumentUrls.Count > 0)
            {
                logger.LogInformation("Adding documents to property For User {UserId} with Property {PropertyId}", user.Id, property.Id);
                request.DocumentUrls.ForEach(item => property.AddDocument(item));
            }

             unitOfWork.Repository<PropertyAggregate>().Add(property);

            var assignment = await lawyerAssignmentService.PickNextVerifiedLawyerAsync(cancellationToken);
            if (assignment.IsSuccess)
            {
                property.AssignVerificationLawyer(assignment.Value);
            }
            else
            {
                logger.LogInformation("No verified lawyer available; property {PropertyId} queued for manual assignment", property.Id);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Ok(property.Id);
        }
    }
}
