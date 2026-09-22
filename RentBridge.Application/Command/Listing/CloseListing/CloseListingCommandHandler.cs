using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    public class CloseListingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CloseListingCommandHandler> logger) : IRequestHandler<CloseListingCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CloseListingCommand request, CancellationToken cancellationToken)
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

            if (listing.OwnerUserId != user.Id)
            {
                logger.LogInformation("User {userId} is not the owner of listing {listingId}", user.Id, request.ListingId);
                return Result<Guid>.Fail("You can only close your own listing");
            }

            var closed = listing.Close();
            if (!closed.IsSuccess)
            {
                logger.LogInformation("Listing {listingId} cannot be closed: {error}", request.ListingId, closed.Error);
                return Result<Guid>.Fail(closed.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(listing.Id);
        }
    }
}
