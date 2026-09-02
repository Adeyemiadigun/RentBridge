using Microsoft.AspNetCore.Http;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Outbound port to the KYC vendor (Dojah / Smile Identity / Prembly).
/// Implemented in Infrastructure; the outcome arrives back via webhook
/// and is applied through ApplyIdentityResult / ApplyFacialResult on the
/// KycVerification aggregate.
/// </summary>
public interface IIdentityVerificationService
{
    /// <summary>Initiate a NIN lookup for a user. Async; result comes back via webhook.</summary>
    Task SubmitNinAsync(NinNumber nin,IFormFile facialImage, CancellationToken ct);

    /// <summary>Initiate facial verification for a user. Async; result via webhook.</summary>
    Task SubmitFacialAsync(NinNumber nin, CancellationToken ct);
}
