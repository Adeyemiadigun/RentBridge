using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using  ListingAggreagte = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Command.Listing
{
    public class ListingPropertyCommandHandler(IUnitOfWork unitOfWork, ICurrentUser _currentUser, ILogger<ListingPropertyCommandHandler> logger) : IRequestHandler<ListPropertyCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(ListPropertyCommand request, CancellationToken cancellationToken)
        {
            var res = await _currentUser.GetCurrentUser(true, cancellationToken);
            if(!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }
            var user = res.Value;
            var property = await unitOfWork.Repository<Domain.Aggregates.Property>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
            if(property == null)
            {
                logger.LogInformation("Property not found {propertyId}",request.PropertyId);
                return Result<Guid>.Fail("Property not found");
            }
            if(property.OwnerUserId != user.Id)
            {
                logger.LogInformation("User {userId} is not the owner of property {propertyId}", user.Id, request.PropertyId);
                return Result<Guid>.Fail("You can only list your own property");
            }
            if(!property.IsVerified)
            {
                logger.LogInformation("Property Has not been Verified");
                return Result<Guid>.Fail("Property Has not been Verified");
            }
            var money = Money.Naira(request.PriceAmount);
            var propertyListing = new ListingAggreagte(user.Id,property.Id,request.Title,money,request.Description);

             unitOfWork.Repository<ListingAggreagte>().Add(propertyListing);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Ok(propertyListing.Id);
        }
    }
}
