using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Properties;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Query.Property;

public class GetPropertyReviewsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetPropertyReviewsQueryHandler> logger)
    : IRequestHandler<GetPropertyReviewsQuery, Result<IReadOnlyList<PropertyReviewItem>>>
{
    public async Task<Result<IReadOnlyList<PropertyReviewItem>>> Handle(
        GetPropertyReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<IReadOnlyList<PropertyReviewItem>>.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {userId} is not authorized to browse property reviews", user.Id);
            return Result<IReadOnlyList<PropertyReviewItem>>.Fail("Only a lawyer or admin can browse property reviews.");
        }

        var page = await unitOfWork.Repository<PropertyAggregate>().GetPagedAsync(
            user.Role == UserRole.Admin ? null : p => p.VerificationLawyerId == user.Id,
            page: 1,
            pageSize: 500,
            includes: [p => p.Documents],
            ct: cancellationToken);

        var properties = page.Items;

        var userIds = properties
            .Select(p => p.OwnerUserId)
            .Concat(properties.Select(p => p.VerificationLawyerId ?? Guid.Empty))
            .Distinct()
            .ToList();

        var owners = await unitOfWork.Repository<Domain.Aggregates.User>().FindAsync(
            u => userIds.Contains(u.Id),
            cancellationToken);

        var usersById = owners.ToDictionary(u => u.Id);

        var items = properties.Select(p =>
        {
            Guid? lawyerId = p.VerificationLawyerId;
            Domain.Aggregates.User? lawyer = lawyerId.HasValue && usersById.TryGetValue(lawyerId.Value, out var lw) ? lw : null;
            var owner = usersById.TryGetValue(p.OwnerUserId, out var ow) ? ow : null;

            return new PropertyReviewItem(
                p.Id,
                p.PropertyAddress.Street,
                p.PropertyAddress.City,
                p.PropertyAddress.Area,
                p.PropertyAddress.State,
                p.PropertyType,
                p.Bedrooms,
                p.Bathrooms,
                p.IsVerified,
                p.VerifiedByUserId,
                p.VerifiedByName,
                p.VerifiedByRole,
                p.VerificationLawyerId,
                lawyer is null ? null : $"{lawyer.FirstName} {lawyer.LastName}".Trim(),
                p.OwnerUserId,
                owner is null ? string.Empty : $"{owner.FirstName} {owner.LastName}".Trim(),
                p.Documents.Select(d => new OwnershipDocumentReviewItem(
                    d.Id,
                    d.FileKey,
                    d.Status.ToString(),
                    d.VerifiedByName,
                    d.VerifiedByRole,
                    d.RejectionReason,
                    d.UploadedAt,
                    d.ReviewedAt)).ToList())
            ;
        }).ToList();

        return Result<IReadOnlyList<PropertyReviewItem>>.Ok(items);
    }
}