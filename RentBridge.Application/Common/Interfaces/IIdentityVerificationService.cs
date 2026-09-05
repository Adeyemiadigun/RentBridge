using Microsoft.AspNetCore.Http;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Outbound port to the identity-verification vendor (Smile ID).
/// Implemented in Infrastructure. Smile ID runs verification asynchronously:
/// we submit one biometric_kyc job (NIN + selfie) and the outcome arrives on
/// our webhook, which is applied through ApplyResult on the KycVerification
/// aggregate.
/// </summary>
public interface IIdentityVerificationService
{
    /// <summary>
    /// Submits a single Smile ID biometric_kyc job covering BOTH checks: the NIN
    /// (ID-number lookup) and the selfie (liveness + face match). One job, one
    /// webhook, one complete result. Returns the Smile job id on success.
    /// NOTE: biometric_kyc requires 6-8 liveness frames plus the selfie image;
    /// a single selfie alone cannot satisfy the liveness check.
    /// </summary>
    Task<Result<string?>> SubmitVerificationAsync(
        Guid kycId,
        NinNumber nin,
        VerificationSubject subject,
        IFormFile selfieImage,
        IReadOnlyList<IFormFile> livenessImages,
        CancellationToken ct);
}