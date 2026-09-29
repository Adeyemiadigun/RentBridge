using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Listing
{
    public record PublishListingCommandHandler(ICurrentUser _currentUser, ILogger<PublishListingCommandHandler> logger, IUnitOfWork unitOfWork) : IRequestHandler<PublishListingCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(PublishListingCommand request, CancellationToken cancellationToken)
        {
            var res = await _currentUser.GetCurrentUser(true, cancellationToken);
            if (!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }
            var user = res.Value;
            var listing = await unitOfWork.Repository<Domain.Aggregates.Listing>().FirstOrDefault(p => p.Id == request.ListingId, cancellationToken);
            if (listing == null)
            {
                logger.LogInformation("Listing {listingId} not found", request.ListingId);
                return Result<Guid>.Fail("Listing not found");
            }
            if (listing.OwnerUserId != user.Id && user.Role != RentBridge.Domain.Enums.UserRole.Admin)
            {
                logger.LogInformation("User {userId} is not the owner of listing {listingId}", user.Id, request.ListingId);
                return Result<Guid>.Fail("You can only publish your own listing");
            }
            // Owner onboarding gates only apply to the publisher; an admin moderating
            // another owner's listing acts on behalf of the platform.
            if (user.Role != RentBridge.Domain.Enums.UserRole.Admin)
            {
                if (!user.IdentityVerified)
                {
                    logger.LogInformation("User {userId} is not identity verified", user.Id);
                    return Result<Guid>.Fail("Your identity must be verified before you can publish a listing");
                }
                if (user.PayoutAccount is not { IsActive: true })
                {
                    logger.LogInformation("User {userId} has no payout account", user.Id);
                    return Result<Guid>.Fail("Add a verified payout bank account before you can publish a listing");
                }
            }

            var property = await unitOfWork.Repository<Domain.Aggregates.Property>().FirstOrDefault(p => p.Id == listing.PropertyId, cancellationToken);
            if (property == null || !property.IsVerified)
            {
                logger.LogInformation("Property {propertyId} for listing {listingId} has not been verified", listing.PropertyId, request.ListingId);
                return Result<Guid>.Fail("Property must be verified before the listing can be published");
            }

            var publishRes = listing.Publish();
            if (!publishRes.IsSuccess)
            {
                logger.LogInformation("Listing {listingId} cannot be published: {error}", request.ListingId, publishRes.Error);
                return Result<Guid>.Fail(publishRes.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(listing.Id);
        }
    }
}