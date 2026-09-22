namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// Dojah configuration. Binds the "Dojah" section.
/// Production via Dojah__AppId / Dojah__SecretKey env vars.
/// See https://docs.dojah.io/api-reference/get-started/authentication
/// </summary>
public sealed class DojahOptions
{
    public const string SectionName = "Dojah";

    /// <summary>App ID from Developers → Configuration in the Dojah dashboard.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Secret key, sent raw in the Authorization header (never "Bearer").</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Legacy alias for SecretKey.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Explicit base URL override. Otherwise derived from Environment.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>"sandbox" → https://sandbox.dojah.io, "production" → https://api.dojah.io.</summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Minimum selfie-match score (50–100) for match=true. Dojah defaults to 90.
    /// </summary>
    public int Threshold { get; set; } = 90;

    public string ResolvedSecretKey =>
        !string.IsNullOrWhiteSpace(SecretKey) ? SecretKey : ApiKey;

    public string ResolvedBaseUrl =>
        !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl.TrimEnd('/')
            : string.Equals(Environment?.Trim(), "production", StringComparison.OrdinalIgnoreCase)
                ? "https://api.dojah.io"
                : "https://sandbox.dojah.io";
}
