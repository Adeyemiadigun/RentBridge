using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces.Verification;

/// <summary>
/// Strategy contract for identity-verification vendors (Dojah, Smile ID, ...).
/// Each provider implements the capabilities it supports; unsupported
/// operations return a failed <see cref="Result{T}"/> instead of throwing,
/// so the caller can fall back or surface a clean error.
/// </summary>
public interface IIdentityVerificationProvider
{
    /// <summary>Vendor key, e.g. "dojah" or "smile". Matches Verification:Provider config.</summary>
    string ProviderName { get; }

    /// <summary>True when the provider verifies NIN+selfie synchronously (Dojah).</summary>
    bool SupportsSyncVerification { get; }

    /// <summary>True when the provider mints a client SDK session (Smile ID).</summary>
    bool SupportsSdkSession { get; }

    /// <summary>
    /// Synchronously verifies an identity (NIN + selfie face-match).
    /// Returns the verdict; a failed result means "could not verify"
    /// (vendor error, unknown NIN) — the KYC record must stay Pending.
    /// </summary>
    Task<Result<ProviderVerificationOutcome>> VerifyAsync(
        IdentityVerificationRequest request, CancellationToken ct);

    /// <summary>
    /// Builds the client bootstrap for SDK/widget flows (Dojah EasyOnboard,
    /// Smile ID SDK): the frontend opens the vendor widget with the returned
    /// data, captures biometrics on-device, and the verdict arrives later on
    /// the provider webhook. No images travel through our API.
    /// </summary>
    Task<Result<SdkSession>> CreateSdkSessionAsync(
        Guid kycVerificationId, string nin, CancellationToken ct);
}
