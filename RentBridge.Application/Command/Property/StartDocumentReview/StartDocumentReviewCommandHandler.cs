using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class StartDocumentReviewCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    IEmailService emailService,
    ILogger<StartDocumentReviewCommandHandler> logger) : IRequestHandler<StartDocumentReviewCommand, Result>
{
    public async Task<Result> Handle(StartDocumentReviewCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {userId} is not authorized to review documents", user.Id);
            return Result.Fail("Only a lawyer or admin can submit documents for review.");
        }

        var property = await unitOfWork.Repository<PropertyAggregate>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
        if (property is null)
        {
            logger.LogInformation("Property {propertyId} not found", request.PropertyId);
            return Result.Fail("Property not found");
        }

        var auth = await lawyerService.ResolveAndAuthorizeAsync(property, user, cancellationToken);
        if (!auth.IsSuccess)
        {
            logger.LogInformation("User {userId} is not authorized to review document {documentId} on property {propertyId}: {error}", user.Id, request.DocumentId, request.PropertyId, auth.Error);
            return Result.Fail(auth.Error!);
        }

        var reviewResult = property.StartDocumentReview(request.DocumentId);
        if (!reviewResult.IsSuccess)
        {
            logger.LogInformation("Document {documentId} on property {propertyId} cannot enter review: {error}", request.DocumentId, request.PropertyId, reviewResult.Error);
            return Result.Fail(reviewResult.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == property.OwnerUserId, cancellationToken);
        if (owner is not null)
        {
            var document = property.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
            var documentLink = document is not null
                ? $"<a href=\"{document.FileKey}\">View document</a>"
                : "Document";

            var subject = "Your property document is under review";
            var body = $"<h3>Property document under review</h3><p>Hello {owner.FirstName},</p>" +
                       $"<p>Your property document has been submitted for review by our legal team.</p>" +
                       $"<p>Property ID: <strong>{property.Id}</strong></p>" +
                       $"<p>{documentLink}</p>" +
                       $"<p>We will notify you once the review is complete.</p>";
            await emailService.SendEmailAsync(owner.Email.Value, subject, body, cancellationToken);
        }

        return Result.Ok();
    }
}