using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.SubmitIdentityVerification;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Dtos.Kyc;
using RentBridge.Application.Query.Kyc;

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
    /// Starts a verification with the active provider (default Dojah) and
    /// returns the frontend widget bootstrap. The app opens the vendor
    /// widget (Dojah Connect) with this session — selfies and the NIN are
    /// captured on-device and pre-filled from the app, never posted here.
    /// The verdict arrives on the provider webhook.
    /// </summary>
    /// <remarks>
    /// Frontend flow:
    /// 1. POST here with the NIN → receive the session below.
    /// 2. Open Dojah Connect with appId/publicKey/widgetId and
    ///    reference_id = referenceId (our kyc id — do not change it),
    ///    pre-filling gov_data.nin.
    /// 3. On widget onSuccess, poll GET kyc/status until "verified" or
    ///    "rejected". onSuccess alone never proves a pass.
    ///
    /// Sample session for provider "dojah":
    /// {
    ///   "referenceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "data": {
    ///     "appId": "6ab2a920e977f60ebe9cd627",
    ///     "publicKey": "dojah public key (p_key, safe for clients)",
    ///     "widgetId": "published EasyOnboard flow id",
    ///     "environment": "sandbox"
    ///   }
    /// }
    /// </remarks>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyIdentity(
        [FromBody] VerifyIdentityRequest request,
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
            provider = result.Value.Provider,
            status = "pending",
            session = result.Value.Session,
        });
    }

    /// <summary>
    /// Returns the current user's latest KYC state for polling after the
    /// vendor widget completes ("none", "pending", "verified", "rejected").
    /// Never trust the widget's onSuccess alone — only this (webhook-decided)
    /// status proves a pass.
    /// </summary>
    /// <remarks>
    /// Sample response:
    /// { "kycId": "3fa85f64-...", "provider": "dojah",
    ///   "status": "verified", "completedAt": "2026-09-24T10:15:30Z" }
    /// Poll every ~3s after onSuccess (timeout ~2 min). "none" means the user
    /// never started verification.
    /// </remarks>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized(new { error = "Authentication required." });
        }

        var result = await mediator.Send(new GetMyVerificationStatusQuery(), ct);

        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
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

        var data = result.Value.Session?.Data;
        string? Value(string key) =>
            data is not null && data.TryGetValue(key, out var value) ? value : null;

        return Ok(new
        {
            kycId = result.Value.KycVerificationId,
            token = Value("token"),
            partnerId = Value("partnerId") ?? configuration["Smile:PartnerId"],
            environment = Value("environment") ?? configuration["Smile:Environment"],
            country = Value("country"),
            idType = Value("idType"),
            idNumber = Value("idNumber") ?? request.Nin,
            callbackUrl = Value("callbackUrl") ?? configuration["Smile:CallbackUrl"],
            privacyPolicyUrl = Value("privacyPolicyUrl") ?? configuration["Smile:PrivacyPolicyUrl"],
            productType = Value("productType"),
        });
    }
}
