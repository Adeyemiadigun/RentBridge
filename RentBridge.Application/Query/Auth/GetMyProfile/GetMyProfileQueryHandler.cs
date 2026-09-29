using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Common;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Query.Auth;

public sealed class GetMyProfileQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetMyProfileQueryHandler> logger)
    : IRequestHandler<GetMyProfileQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<UserProfileResponse>.Fail(res.Error!);
        }
        var current = res.Value;

        var user = await unitOfWork.Repository<UserAggregate>()
            .GetByIdAsync(current.Id, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Current user {UserId} not found", current.Id);
            return Result<UserProfileResponse>.Fail("User not found.");
        }

        var profile = new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email.Value,
            user.Phone.Value,
            user.Role.ToString(),
            user.IdentityVerified);

        return Result<UserProfileResponse>.Ok(profile);
    }
}