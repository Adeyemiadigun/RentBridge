namespace RentBridge.Application.Common.Options;

/// <summary>
/// Selects the active identity-verification provider.
/// Production via the Verification__Provider environment variable.
/// </summary>
public sealed class VerificationOptions
{
    public const string SectionName = "Verification";

    /// <summary>Matches IIdentityVerificationProvider.ProviderName. Default "dojah".</summary>
    public string Provider { get; set; } = "dojah";
}
