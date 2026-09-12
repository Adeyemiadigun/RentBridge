using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Command.Lease;

public class CreateLeaseCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CreateLeaseCommandHandler> logger)
    : IRequestHandler<CreateLeaseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLeaseCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<Guid>.Fail(res.Error!);
        }
        var user = res.Value;

        var listing = await unitOfWork.Repository<ListingAggregate>()
            .FirstOrDefault(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            logger.LogInformation("Listing {listingId} not found", request.ListingId);
            return Result<Guid>.Fail("Listing not found");
        }

        if (listing.Status != ListingStatus.Published)
        {
            logger.LogInformation("Listing {listingId} is not published", request.ListingId);
            return Result<Guid>.Fail("You can only rent a published listing");
        }

        if (listing.OwnerUserId == user.Id)
        {
            logger.LogInformation("User {userId} attempted to rent their own listing {listingId}", user.Id, request.ListingId);
            return Result<Guid>.Fail("You cannot rent your own listing");
        }

        var lease = new Lease(listing.Id, user.Id, listing.OwnerUserId);
        unitOfWork.Repository<Lease>().Add(lease);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(lease.Id);
    }
}