using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Properties;
using RentBridge.Domain.Common;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;

namespace RentBridge.Application.Query.Property;

public class GetUserPropertiesQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetUserPropertiesQueryHandler> logger) : IRequestHandler<GetUserPropertiesQuery, Result<PagedResult<PropertyItem>>>
{
    public async Task<Result<PagedResult<PropertyItem>>> Handle(GetUserPropertiesQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PagedResult<PropertyItem>>.Fail(res.Error!);
        }
        var user = res.Value;

        if (!PropertyAggregate.CanCreateBy(user.Role))
        {
            logger.LogInformation("User {userId} is not allowed to view properties", user.Id);
            return Result<PagedResult<PropertyItem>>.Fail("User is not allowed to view properties");
        }

        var paged = await unitOfWork.Repository<PropertyAggregate>().GetPagedAsync(
            p => p.OwnerUserId == user.Id
                && (request.IsVerified == null || p.IsVerified == request.IsVerified)
                && (request.State == null || p.PropertyAddress.State == request.State)
                && (request.City == null || p.PropertyAddress.City == request.City),
            request.Page,
            request.PageSize,
            p => p.Id,
            true,
            cancellationToken,
            p => p.Documents);

        var items = paged.Items
            .Select(p => new PropertyItem(
                p.Id,
                p.PropertyAddress.Street,
                p.PropertyAddress.City,
                p.PropertyAddress.Area,
                p.PropertyAddress.State,
                p.IsVerified,
                p.Documents.Select(d => new OwnershipDocumentItem(d.Id, d.Status, d.FileKey)).ToList()))
            .ToList();

        return Result<PagedResult<PropertyItem>>.Ok(
            new PagedResult<PropertyItem>(paged.Page, paged.PageSize, paged.TotalCount, items));
    }
}