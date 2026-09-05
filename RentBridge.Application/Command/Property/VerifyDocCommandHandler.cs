using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Property
{
    public class VerifyDocCommandHandler(ICurrentUser currentUser,ILogger<VerifyDocCommandHandler> logger,IUnitOfWork unitOfWork) : IRequestHandler<VerifyDocCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(VerifyDocCommand request, CancellationToken cancellationToken)
        {
            var res = await currentUser.GetCurrentUser(true);
            if (!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }
            var user = res.Value;

            if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
            {
                logger.LogInformation("User {userId} is not authorized to review documents", user.Id);
                return Result<Guid>.Fail("Only a lawyer or admin can submit documents for review.");
            }
            var property = await unitOfWork.Repository<Domain.Aggregates.Property>().FirstOrDefault(p => p.Documents.Any(d => d.Id == request.DocumentId), cancellationToken);

            if (property == null)
            {
                logger.LogInformation("Property Belonging to Document {docId} not found",request.DocumentId);
                return Result<Guid>.Fail("Property not found");
            }
            var result = property.VerifyDocument(request.DocumentId);
            if (!result.IsSuccess)
            {
                logger.LogInformation("Failed to verify document {documentId} for property {propertyId}: {error}", request.DocumentId, property.Id, result.Error);
                return Result<Guid>.Fail(result.Error!);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Document {documentId} for property {propertyId} verified successfully by user {userId}", request.DocumentId, property.Id, user.Id);
            return Result<Guid>.Ok(property.Id);
        }
    }
}
