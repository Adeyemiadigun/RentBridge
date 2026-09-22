namespace RentBridge.Application.Command.SubmitIdentityVerification;

/// <summary>
/// Provider-agnostic result of starting a KYC flow.
/// Sync providers (Dojah) return the verdict inline (Passed/Confidence set,
/// ClientToken null). SDK providers (Smile) return a ClientToken and the
/// verdict arrives later on the provider webhook (Passed null = pending).
/// </summary>
public sealed record KycSubmissionResponse(
    Guid KycVerificationId,
    string Provider,
    bool? Passed,
    double? Confidence,
    string? ProviderRef,
    string? ClientToken);
