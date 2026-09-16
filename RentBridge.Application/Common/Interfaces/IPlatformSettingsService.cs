using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

public interface IPlatformSettingsService
{
    /// <summary>
    /// Returns the singleton platform settings, seeding the default row on first access.
    /// </summary>
    Task<Result<Domain.Aggregates.PlatformSettings>> GetOrCreateAsync(CancellationToken cancellationToken);
}