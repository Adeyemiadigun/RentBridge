using MediatR;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.SubmitIdentityVerification;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.Kyc;

namespace RentBridge.Api.Controllers;

[ApiController]
[Route("api/kyc")]
public sealed class KycController(
    IMediator mediator,
    ICurrentUser currentUser,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Starts a Smile ID biometric_kyc flow. Creates the KycVerification
    /// record, mints a short-lived Smile token, and returns everything the
    /// client needs to bootstrap the Smile SDK.
    /// </summary>
    [HttpPost("smile-token")]
    public async Task<IActionResult> StartSmileVerification(
        [FromBody] StartSmileVerificationRequest request,
        CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized(new { error = "Authentication required." });
        }

        var cmd = new SubmitIdentityVerificationCommand(request.Nin);
        var result = await mediator.Send(cmd, ct);

        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            kycId = result.Value.KycVerificationId,
            token = result.Value.SmileToken,
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