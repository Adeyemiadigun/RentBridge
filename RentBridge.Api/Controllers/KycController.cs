using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.SubmitIdentityVerification;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.Kyc;

namespace RentBridge.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/kyc")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class KycController(
    IMediator mediator,
    ICurrentUser currentUser,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Verifies NIN + selfie with the active provider (default Dojah).
    /// Sync providers return the verdict inline; the KycVerification
    /// record is created and decided in one call.
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyIdentity(
        [FromBody] VerifyIdentityRequest request,
        CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized(new { error = "Authentication required." });
        }

        var cmd = new SubmitIdentityVerificationCommand(
            request.Nin, request.SelfieImage, request.FirstName, request.LastName);
        var result = await mediator.Send(cmd, ct);

        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        var status = result.Value.Passed is null
            ? "pending"
            : result.Value.Passed.Value ? "verified" : "rejected";

        return Ok(new
        {
            kycId = result.Value.KycVerificationId,
            provider = result.Value.Provider,
            passed = result.Value.Passed,
            confidence = result.Value.Confidence,
            providerRef = result.Value.ProviderRef,
            status,
        });
    }

    /// <summary>
    /// Legacy Smile ID biometric_kyc flow. Creates the KycVerification
    /// record, mints a short-lived Smile token, and returns everything the
    /// client needs to bootstrap the Smile SDK. Prefer POST verify (Dojah).
    /// </summary>
    [HttpPost("smile-token")]
    [Obsolete("Use POST verify with the default (Dojah) provider instead.")]
    public async Task<IActionResult> StartSmileVerification(
        [FromBody] StartSmileVerificationRequest request,
        CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized(new { error = "Authentication required." });
        }

        var cmd = new SubmitIdentityVerificationCommand(request.Nin, Provider: "smile");
        var result = await mediator.Send(cmd, ct);

        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            kycId = result.Value.KycVerificationId,
            token = result.Value.ClientToken,
            partnerId = configuration["Smile:PartnerId"],
            environment = configuration["Smile:Environment"],
            country = "NG",
            idType = "NIN",
            idNumber = request.Nin,
            callbackUrl = configuration["Smile:CallbackUrl"],
            privacyPolicyUrl = configuration["Smile:PrivacyPolicyUrl"],
            productType = "biometric_kyc",
        });
    }
}
