using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class VerifyPropertyCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEmailService emailService,
    ILogger<VerifyPropertyCommandHandler> logger) : IRequestHandler<VerifyPropertyCommand, Result>
{
    public async Task<Result> Handle(VerifyPropertyCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {userId} is not authorized to verify a property", user.Id);
            return Result.Fail("Only a lawyer or admin can verify a property.");
        }

        var property = await unitOfWork.Repository<PropertyAggregate>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {propertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        var verifyResult = property.MarkOwnershipVerified();
        if (!verifyResult.IsSuccess)
        {
            logger.LogInformation("Property {propertyId} cannot be verified: {error}", request.PropertyId, verifyResult.Error);
            return Result.Fail(verifyResult.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == property.OwnerUserId, cancellationToken);
        if (owner is not null)
        {
            var subject = "Your property has been verified";
            var body = $"<h3>Property verified</h3><p>Hello {owner.FirstName},</p>" +
                       $"<p>Your property has been <strong>verified</strong> by our legal team.</p>" +
                       $"<p>Property ID: <strong>{property.Id}</strong></p>" +
                       $"<p>You can now create a listing for this property.</p>";

            await emailService.SendEmailAsync(owner.Email.Value, subject, body, cancellationToken);
        }

        return Result.Ok();
    }
}