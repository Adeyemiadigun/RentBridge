using RentBridge.Application.Common.Interfaces.Verification;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

/// <summary>
/// Provider-agnostic result of starting a KYC flow. Session carries the
/// frontend SDK/widget bootstrap (Dojah: appId/publicKey/widgetId/
/// referenceId; Smile: token/partnerId/...). The verdict arrives later on
/// the provider webhook (Passed null = pending).
/// </summary>
public sealed record KycSubmissionResponse(
    Guid KycVerificationId,
    string Provider,
    bool? Passed,
    double? Confidence,
    string? ProviderRef,
    SdkSession? Session);
