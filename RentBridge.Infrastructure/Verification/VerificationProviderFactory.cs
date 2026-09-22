using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Application.Common.Options;

namespace RentBridge.Infrastructure.Verification;

/// <summary>
/// Default <see cref="IIdentityVerificationProviderFactory"/>.
/// Resolves providers registered as IEnumerable{IIdentityVerificationProvider}.
/// </summary>
public sealed class VerificationProviderFactory(
    IEnumerable<IIdentityVerificationProvider> providers,
    IOptions<VerificationOptions> options) : IIdentityVerificationProviderFactory
{
    public IIdentityVerificationProvider GetDefault() => Get(options.Value.Provider);

    public IIdentityVerificationProvider Get(string? providerName)
    {
        var key = (providerName ?? options.Value.Provider ?? "dojah").Trim().ToLowerInvariant();
        var all = providers.ToList();
        return all.FirstOrDefault(p =>
                p.ProviderName.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Unknown verification provider '{providerName}'. " +
                $"Available: {string.Join(", ", all.Select(p => p.ProviderName))}. " +
                $"Set Verification:Provider to one of them.");
    }
}
