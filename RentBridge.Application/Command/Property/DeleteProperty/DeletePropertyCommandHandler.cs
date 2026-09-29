using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyEntity = RentBridge.Domain.Aggregates.Property;

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

        unitOfWork.Repository<PropertyEntity>().Remove(property);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Property {PropertyId} deleted by user {UserId}", property.Id, user.Id);
        return Result.Ok();
    }
}