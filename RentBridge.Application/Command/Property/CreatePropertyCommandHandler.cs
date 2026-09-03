using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property
{
    public class CreatePropertyCommandHandler(IUnitOfWork unitOfWork,ICurrentUser _currentUser,ILogger<CreatePropertyCommandHandler> logger) : IRequestHandler<CreatePropertyCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if (userId is null)
            {
                logger.LogInformation("User not authenticated");
                return Result<Guid>.Fail("User not Authenticated");
            }
                
            var user = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == _currentUser.UserId, cancellationToken);
            if(user == null)
            {
                logger.LogInformation("User not found");
                return Result<Guid>.Fail("User not found");
            }
            if (!user.IdentityVerified)
            {
                logger.LogInformation("User identity not verified");
                return Result<Guid>.Fail("User identity not verified");
            }
            
            if(!PropertyAggregate.CanCreateBy(user.Role))
            {
                logger.LogInformation("User is not allowed to create property {UserId}", userId.Value);
                return Result<Guid>.Fail("User is not allowed to create property");
            }

            var property = new PropertyAggregate(userId.Value, request.Street, request.City, request.Area, request.State);

            if (request.DocumentUrls.Count > 0)
            {
                logger.LogInformation("Adding documents to property For User {UserId} with Property {PropertyId}", userId.Value, property.Id);
                request.DocumentUrls.ForEach(item => property.AddDocument(item));
            }

             unitOfWork.Repository<PropertyAggregate>().Add(property);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Ok(property.Id);
        }
    }
}
