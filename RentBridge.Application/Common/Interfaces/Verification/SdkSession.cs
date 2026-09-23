namespace RentBridge.Application.Common.Interfaces.Verification;

/// <summary>
/// Opaque client bootstrap for SDK/widget flows. The backend creates the
/// pending KYC record and returns everything the frontend SDK needs to open
/// the vendor widget; biometrics are captured on-device and the verdict
/// arrives later on the provider webhook. ReferenceId is our KycVerification
/// id so the webhook can correlate the event.
/// </summary>
public sealed record SdkSession(
    string ReferenceId,
    IReadOnlyDictionary<string, string?> Data);
