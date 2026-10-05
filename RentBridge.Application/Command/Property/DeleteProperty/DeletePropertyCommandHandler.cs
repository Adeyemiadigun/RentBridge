using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyEntity = RentBridge.Domain.Aggregates.Property;
using ListingEntity = RentBridge.Domain.Aggregates.Listing;
using LeaseEntity = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Property;

public class DeletePropertyCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    ILogger<DeletePropertyCommandHandler> logger) : IRequestHandler<DeletePropertyCommand, Result>
{
    public async Task<Result> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var property = await unitOfWork.Repository<PropertyEntity>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {PropertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        // Ensure user owns this property
        if (property.OwnerUserId != user.Id && user.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to delete property {PropertyId} they don't own", user.Id, request.PropertyId);
            return Result.Forbid("You can only delete your own properties.");
        }

        // Delete images from Cloudinary
        foreach (var imageUrl in property.Images)
        {
            try
            {
                await fileStorage.DeleteAsync(imageUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete image {ImageUrl} from Cloudinary", imageUrl);
            }
        }

        // Delete documents from Cloudinary
        foreach (var document in property.Documents)
        {
            try
            {
                await fileStorage.DeleteAsync(document.FileKey, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete document {FileKey} from Cloudinary", document.FileKey);
            }
        }

        // Cascade: a property's listings and leases are only reachable through it, and
        // nothing in the schema enforces that (the PropertyId/ListingId foreign keys are
        // unenforced), so removing the property alone would leave them behind as permanent
        // orphans that still show up in listings, leases and the admin KPIs.
        var listings = await unitOfWork.Repository<ListingEntity>()
            .FindAsync(l => l.PropertyId == property.Id, cancellationToken);

        if (listings.Count > 0)
        {
            var listingIds = listings.Select(l => l.Id).ToList();
            var leases = await unitOfWork.Leases.GetByListingIdsWithEscrowPaymentsAsync(listingIds, cancellationToken);

            // Escrow that is funded, mid-payout or stuck still represents a real obligation:
            // the tenant's money is held or the landlord payout has not landed. Deleting the
            // property would strand that payout trail, so the delete is refused until it settles.
            var unsettled = leases
                .SelectMany(l => l.EscrowPayments)
                .Where(p => p.Status is EscrowStatus.Funded or EscrowStatus.Releasing or EscrowStatus.PayoutFailed)
                .ToList();

            if (unsettled.Count > 0)
            {
                logger.LogInformation(
                    "Property {PropertyId} delete refused: {Count} escrow payment(s) not yet settled",
                    property.Id,
                    unsettled.Count);
                return Result.Fail(
                    "This property has an escrow payment that is not settled yet. "
                    + "Wait for the payout to complete (or contact support) before deleting the property.");
            }

            // Pre-money leases and settled (Released/Refunded) leases carry no outstanding
            // obligation. LedgerEntry rows are a separate table and are never removed, so the
            // transaction history for a paid property survives the delete.
            foreach (var lease in leases)
            {
                unitOfWork.Repository<LeaseEntity>().Remove(lease);
            }

            foreach (var listing in listings)
            {
                unitOfWork.Repository<ListingEntity>().Remove(listing);
            }
        }

        unitOfWork.Repository<PropertyEntity>().Remove(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Property {PropertyId} deleted by user {UserId}", property.Id, user.Id);
        return Result.Ok();
    }
}