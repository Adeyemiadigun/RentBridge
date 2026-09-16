using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Command.Property;

public class VerifyDocumentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    IEmailService emailService,
    ILogger<VerifyDocumentCommandHandler> logger) : IRequestHandler<VerifyDocumentCommand, Result>
{
    public async Task<Result> Handle(VerifyDocumentCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {userId} is not authorized to verify documents", user.Id);
            return Result.Fail("Only a lawyer or admin can verify documents.");
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
            logger.LogInformation("User {userId} is not authorized to verify document {documentId} on property {propertyId}: {error}", user.Id, request.DocumentId, request.PropertyId, auth.Error);
            return Result.Fail(auth.Error!);
        }

        var verifyResult = property.VerifyDocument(request.DocumentId);
        if (!verifyResult.IsSuccess)
        {
            logger.LogInformation("Document {documentId} on property {propertyId} cannot be verified: {error}", request.DocumentId, request.PropertyId, verifyResult.Error);
            return Result.Fail(verifyResult.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await unitOfWork.Repository<User>().FirstOrDefault(u => u.Id == property.OwnerUserId, cancellationToken);
        if (owner is not null)
        {
            var document = property.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
            var documentLink = document is not null
                ? $"<a href=\"{document.FileKey}\">View document</a>"
                : "Document";

            var subject = "Your property document has been verified";
            var body = $"<h3>Property document verified</h3><p>Hello {owner.FirstName},</p>" +
                       $"<p>Your property document has been <strong>verified</strong> by our legal team.</p>" +
                       $"<p>Property ID: <strong>{property.Id}</strong></p>" +
                       $"<p>{documentLink}</p>";

            await emailService.SendEmailAsync(owner.Email.Value, subject, body, cancellationToken);
        }

        return Result.Ok();
    }
}