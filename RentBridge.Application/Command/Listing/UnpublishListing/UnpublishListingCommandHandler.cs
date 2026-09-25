using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    public class UnpublishListingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UnpublishListingCommandHandler> logger) : IRequestHandler<UnpublishListingCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(UnpublishListingCommand request, CancellationToken cancellationToken)
        {
            var res = await currentUser.GetCurrentUser(false, cancellationToken);
            if (!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }
            var user = res.Value;

            var listing = await unitOfWork.Repository<Domain.Aggregates.Listing>()
                .FirstOrDefault(l => l.Id == request.ListingId, cancellationToken);
            if (listing is null)
            {
                logger.LogInformation("Listing {listingId} not found", request.ListingId);
                return Result<Guid>.Fail("Listing not found");
            }

            if (listing.OwnerUserId != user.Id && user.Role != RentBridge.Domain.Enums.UserRole.Admin)
            {
                logger.LogInformation("User {userId} is not the owner of listing {listingId}", user.Id, request.ListingId);
                return Result<Guid>.Fail("You can only unpublish your own listing");
            }

            var unpublished = listing.Unpublish();
            if (!unpublished.IsSuccess)
            {
                logger.LogInformation("Listing {listingId} cannot be unpublished: {error}", request.ListingId, unpublished.Error);
                return Result<Guid>.Fail(unpublished.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(listing.Id);
        }
    }
}
