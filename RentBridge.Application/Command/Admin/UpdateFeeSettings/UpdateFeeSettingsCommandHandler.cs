using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using SettingsAggregate = RentBridge.Domain.Aggregates.PlatformSettings;

namespace RentBridge.Application.Command.Admin;

public sealed class UpdateFeeSettingsCommandHandler(
    ICurrentUser currentUser,
    IPlatformSettingsService settingsService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateFeeSettingsCommandHandler> logger)
    : IRequestHandler<UpdateFeeSettingsCommand, Result<FeeSettingsResponse>>
{
    public async Task<Result<FeeSettingsResponse>> Handle(UpdateFeeSettingsCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<FeeSettingsResponse>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to update fee settings without admin role", actor.Id);
            return Result<FeeSettingsResponse>.Fail("Only an admin can update fee settings.");
        }

        var settingsRes = await settingsService.GetOrCreateAsync(cancellationToken);
        if (!settingsRes.IsSuccess)
        {
            return Result<FeeSettingsResponse>.Fail(settingsRes.Error!);
        }

        var update = settingsRes.Value.Update(request.PlatformCommissionRate, request.LegalFeeRate);
        if (!update.IsSuccess)
        {
            return Result<FeeSettingsResponse>.Fail(update.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<FeeSettingsResponse>.Ok(ToResponse(settingsRes.Value));
    }

    private static FeeSettingsResponse ToResponse(SettingsAggregate settings)
        => new(settings.PlatformCommissionRate, settings.LegalFeeRate, settings.UpdatedAt);
}