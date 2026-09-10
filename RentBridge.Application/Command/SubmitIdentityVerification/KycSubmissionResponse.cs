namespace RentBridge.Application.Command.SubmitIdentityVerification;

/// <summary>
/// Result of starting a KYC flow. The client passes the token (plus its
/// own partner id, job parameters and callback url from the controller)
/// into the Smile ID SDK, which captures the selfie + liveness frames and
/// submits the biometric_kyc job directly. The verdict arrives on our webhook.
/// </summary>
public sealed record KycSubmissionResponse(
    Guid KycVerificationId,
    string SmileToken);