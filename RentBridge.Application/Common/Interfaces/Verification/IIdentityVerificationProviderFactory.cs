namespace RentBridge.Application.Common.Interfaces.Verification;

/// <summary>
/// Selects an <see cref="IIdentityVerificationProvider"/> by name.
/// The default comes from Verification:Provider config ("dojah").
/// To add a vendor: implement the provider interface, register it in
/// Infrastructure DI, and set Verification:Provider to its ProviderName —
/// no caller code changes.
/// </summary>
public interface IIdentityVerificationProviderFactory
{
    IIdentityVerificationProvider GetDefault();

    IIdentityVerificationProvider Get(string? providerName);
}
