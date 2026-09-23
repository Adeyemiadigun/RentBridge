using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Domain.Common;

namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// Default verification strategy: Dojah EasyOnboard widget flow.
/// The backend creates the pending KYC record and returns the widget
/// bootstrap (appId, publicKey, widgetId, reference_id = our kyc id).
/// The frontend opens the Connect widget, which captures the selfie and
/// NIN on-device; the authoritative verdict arrives on the kyc_widget
/// webhook. No images ever travel through our API.
/// See https://docs.dojah.io/api-reference/hosted-flows-easyonboard/launch-a-flow
/// </summary>
public sealed class DojahIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "dojah";
    public bool SupportsSyncVerification => false;
    public bool SupportsSdkSession => true;

    private readonly DojahOptions _options;
    private readonly ILogger<DojahIdentityVerificationProvider> _logger;

    public DojahIdentityVerificationProvider(
        IOptions<DojahOptions> options,
        ILogger<DojahIdentityVerificationProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<Result<ProviderVerificationOutcome>> VerifyAsync(
        IdentityVerificationRequest request, CancellationToken ct) =>
        Task.FromResult(Result<ProviderVerificationOutcome>.Fail(
            "Dojah verifies in the EasyOnboard widget on the frontend; " +
            "no server-side image verification is available. " +
            "Call CreateSdkSessionAsync and open the widget instead."));

    public Task<Result<SdkSession>> CreateSdkSessionAsync(
        Guid kycVerificationId, string nin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) ||
            string.IsNullOrWhiteSpace(_options.PublicKey) ||
            string.IsNullOrWhiteSpace(_options.WidgetId))
        {
            _logger.LogWarning("Dojah widget is not configured (AppId/PublicKey/WidgetId).");
            return Task.FromResult(Result<SdkSession>.Fail(
                "Dojah:AppId / Dojah:PublicKey / Dojah:WidgetId is not configured. " +
                "Publish an EasyOnboard flow in the Dojah dashboard first."));
        }

        var session = new SdkSession(
            kycVerificationId.ToString("D"),
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["appId"] = _options.AppId,
                ["publicKey"] = _options.PublicKey,
                ["widgetId"] = _options.WidgetId,
                ["environment"] = _options.ResolvedEnvironment,
            });

        return Task.FromResult(Result<SdkSession>.Ok(session));
    }
}
