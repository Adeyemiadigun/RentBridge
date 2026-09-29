namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// Dojah configuration. Binds the "Dojah" section.
/// Verification runs in the frontend EasyOnboard widget, so the backend only
/// needs the widget bootstrap values plus webhook/API secrets.
/// Production via Dojah__AppId / Dojah__PublicKey / Dojah__WidgetId env vars.
/// See https://docs.dojah.io/api-reference/hosted-flows-easyonboard/launch-a-flow
/// </summary>
public sealed class DojahOptions
{
    public const string SectionName = "Dojah";

    /// <summary>App ID from Developers → Configuration in the Dojah dashboard.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Public key (p_key) — safe for client-side widget use.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Published EasyOnboard flow id (config.widget_id for Connect).</summary>
    public string WidgetId { get; set; } = string.Empty;

    /// <summary>Secret key for server-side REST calls (raw, never "Bearer").</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Legacy alias for SecretKey.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Dedicated webhook signing secret from the Dojah dashboard.
    /// If set, it is used for x-dojah-signature validation instead of SecretKey.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>"sandbox" or "production". Selects the widget environment.</summary>
    public string Environment { get; set; } = "sandbox";

    public string ResolvedSecretKey =>
        !string.IsNullOrWhiteSpace(SecretKey) ? SecretKey : ApiKey;

    public string ResolvedEnvironment =>
        string.Equals(Environment?.Trim(), "production", StringComparison.OrdinalIgnoreCase)
            ? "production"
            : "sandbox";
}
