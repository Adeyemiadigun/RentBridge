using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.Listing
{
    public class EditListingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<EditListingCommandHandler> logger) : IRequestHandler<EditListingCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(EditListingCommand request, CancellationToken cancellationToken)
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
                return Result<Guid>.Fail("You can only edit your own listing");
            }

            Money? price = null;
            if (request.PriceAmount.HasValue)
            {
                var moneyResult = Money.Naira(request.PriceAmount.Value);
                if (!moneyResult.IsSuccess)
                {
                    logger.LogInformation("Invalid listing price: {error}", moneyResult.Error);
                    return Result<Guid>.Fail(moneyResult.Error!);
                }
                price = moneyResult.Value;
            }

            var updated = listing.UpdateDetails(request.Title, request.Description, price);
            if (!updated.IsSuccess)
            {
                logger.LogInformation("Listing {listingId} cannot be edited: {error}", request.ListingId, updated.Error);
                return Result<Guid>.Fail(updated.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(listing.Id);
        }
    }
}
