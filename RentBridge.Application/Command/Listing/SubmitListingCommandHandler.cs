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
    public record SubmitListingCommandHandler(ICurrentUser _currentUser, ILogger<SubmitListingCommandHandler> logger,IUnitOfWork unitOfWork) : IRequestHandler<SubmitListingCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(SubmitListingCommand request, CancellationToken cancellationToken)
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
            if (listing.OwnerUserId != user.Id)
            {
                logger.LogInformation("User {userId} is not the owner of listing {listingId}", user.Id, request.ListingId);
                return Result<Guid>.Fail("You can only list your own property");
            }

            var listingRes = listing.MarkPendingVerification();
            if(!listingRes.IsSuccess)
            {
                logger.LogInformation("Listing {listingId} cannot be submitted for verification: {error}", request.ListingId, listingRes.Error);
                return Result<Guid>.Fail(listingRes.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(listing.Id);
        }
    }
}
