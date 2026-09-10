using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Outbound port to Smile ID. In the SDK flow the client submits the
/// biometric job directly; we only need to mint the short-lived JWT
/// that authorizes that submission.
/// </summary>
public interface IIdentityVerificationService
{
    /// <summary>
    /// Mints a short-lived v3 JWT that authorizes the client SDK to
    /// submit a biometric_kyc job directly to Smile ID.
    /// </summary>
    Task<Result<string>> GetTokenAsync(CancellationToken ct);
}