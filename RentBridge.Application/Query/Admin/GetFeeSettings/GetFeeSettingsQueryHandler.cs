using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using SettingsAggregate = RentBridge.Domain.Aggregates.PlatformSettings;

namespace RentBridge.Application.Query.Admin;

public sealed class GetFeeSettingsQueryHandler(
    ICurrentUser currentUser,
    IPlatformSettingsService settingsService,
    ILogger<GetFeeSettingsQueryHandler> logger)
    : IRequestHandler<GetFeeSettingsQuery, Result<FeeSettingsResponse>>
{
    public async Task<Result<FeeSettingsResponse>> Handle(GetFeeSettingsQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<FeeSettingsResponse>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to read fee settings without admin role", actor.Id);
            return Result<FeeSettingsResponse>.Fail("Only an admin can view fee settings.");
        }

        var settingsRes = await settingsService.GetOrCreateAsync(cancellationToken);
        if (!settingsRes.IsSuccess)
        {
            return Result<FeeSettingsResponse>.Fail(settingsRes.Error!);
        }

        var settings = settingsRes.Value;
        return Result<FeeSettingsResponse>.Ok(
            new FeeSettingsResponse(settings.PlatformCommissionRate, settings.LegalFeeRate, settings.UpdatedAt));
    }
}