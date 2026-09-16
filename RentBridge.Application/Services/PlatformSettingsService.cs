using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Services;

public sealed class PlatformSettingsService(IUnitOfWork unitOfWork) : IPlatformSettingsService
{
    public async Task<Result<PlatformSettings>> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var id = Guid.Parse(PlatformSettings.SingletonId);
        var settings = await unitOfWork.Repository<PlatformSettings>()
            .FirstOrDefault(s => s.Id == id, cancellationToken);
        if (settings is not null)
        {
            return Result<PlatformSettings>.Ok(settings);
        }

        var created = PlatformSettings.Create(8m, 2m);
        if (!created.IsSuccess)
        {
            return Result<PlatformSettings>.Fail(created.Error!);
        }

        unitOfWork.Repository<PlatformSettings>().Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PlatformSettings>.Ok(created.Value);
    }
}