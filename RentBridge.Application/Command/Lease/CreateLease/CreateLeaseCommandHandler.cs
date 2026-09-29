using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class CreateLeaseCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CreateLeaseCommandHandler> logger)
    : IRequestHandler<CreateLeaseCommand, Result<Guid>>
{
    private const int MaxRetryAttempts = 3;

    public async Task<Result<Guid>> Handle(CreateLeaseCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<Guid>.Fail(res.Error!);
        }
        var user = res.Value;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
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
                    return Result<Guid>.Forbid("You cannot rent your own listing");
                }

                // Check if lease already exists for this listing and user
                var existingLease = await unitOfWork.Repository<LeaseAggregate>()
                    .FirstOrDefault(l => l.ListingId == request.ListingId && l.TenantUserId == user.Id, cancellationToken);
                
                if (existingLease is not null)
                {
                    logger.LogInformation("Lease already exists for listing {listingId} and user {userId}", request.ListingId, user.Id);
                    return Result<Guid>.Ok(existingLease.Id);
                }

                var lease = new LeaseAggregate(listing.Id, user.Id, listing.OwnerUserId);
                unitOfWork.Repository<LeaseAggregate>().Add(lease);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Ok(lease.Id);
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < 2)
            {
                logger.LogWarning("Concurrency conflict on listing {listingId}, attempt {attempt}/3", request.ListingId, attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
                continue;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Max retry attempts reached for listing {listingId}", request.ListingId);
                return Result<Guid>.Fail("The listing was modified by another process. Please try again.");
            }
        }

        return Result<Guid>.Fail("The listing was modified by another process. Please try again.");
    }
}