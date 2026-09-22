using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Query.Admin;

public sealed class GetLawyersQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetLawyersQueryHandler> logger)
    : IRequestHandler<GetLawyersQuery, Result<PagedResult<LawyerPanelItem>>>
{
    public async Task<Result<PagedResult<LawyerPanelItem>>> Handle(
        GetLawyersQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PagedResult<LawyerPanelItem>>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to list lawyers without admin role", actor.Id);
            return Result<PagedResult<LawyerPanelItem>>.Fail("Only an admin can view lawyers.");
        }

        var page = await unitOfWork.Repository<UserAggregate>().GetPagedAsync(
            u => u.Role == UserRole.Lawyer
                && (request.Status == null
                    || (u.LawyerProfile != null && u.LawyerProfile.Status == request.Status)),
            request.Page,
            request.PageSize,
            orderBy: u => u.Email.Value,
            ascending: true,
            ct: cancellationToken);

        var items = page.Items
            .Select(u => new LawyerPanelItem(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email.Value,
                u.LawyerProfile?.BarNumber,
                u.LawyerProfile?.Status,
                u.IdentityVerified,
                u.LawyerProfile?.LastAssignedAt))
            .ToList();

        return Result<PagedResult<LawyerPanelItem>>.Ok(
            new PagedResult<LawyerPanelItem>(page.Page, page.PageSize, page.TotalCount, items));
    }
}
