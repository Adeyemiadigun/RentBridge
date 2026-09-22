namespace RentBridge.Application.Common.Interfaces.Verification;

/// <summary>Provider-agnostic input for a synchronous identity verification.</summary>
public sealed record IdentityVerificationRequest(
    Guid KycVerificationId,
    string Nin,
    string? SelfieImageBase64,
    string? FirstName = null,
    string? LastName = null);
