using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.ApplyVerificationResult;
using RentBridge.Infrastructure.Verification.Providers.Dojah;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Dojah webhook ingress for hosted (EasyOnboard / kyc_widget) sessions.
/// Launch the widget with our KycVerification id as reference_id; Dojah
/// echoes it back so we can correlate the event. Sync API verifications
/// (POST kyc/verify) need no webhook — they return inline.
/// Always answers 200 for authenticated events so Dojah stops retrying.
/// See https://docs.dojah.io/api-reference/core-concepts/webhooks-signatures
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/webhooks/dojah")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class DojahWebhookController(
    IMediator mediator,
    IDojahSignatureValidator signatureValidator,
    ILogger<DojahWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        // 1. Read the raw body once — the v1 signature is over these exact bytes.
        byte[] rawBody;
        using (var ms = new MemoryStream())
        {
            await Request.Body.CopyToAsync(ms, ct);
            rawBody = ms.ToArray();
        }

        var hasV1 = Request.Headers.TryGetValue("x-dojah-signature", out var sigV1);
        var hasV2 = Request.Headers.TryGetValue("x-dojah-signature-v2", out var sigV2);

        var authentic = (hasV1 && signatureValidator.IsValidV1(sigV1.ToString(), rawBody))
            || (hasV2 && signatureValidator.IsValidV2(sigV2.ToString()));
        if (!authentic)
        {
            logger.LogWarning("Dojah webhook signature verification failed.");
            return Unauthorized(new { acknowledged = false });
        }

        // 2. Parse the event. kyc_widget events carry reference_id,
        //    verification_status and the overall status at the top level.
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Dojah webhook payload could not be deserialized.");
            return Ok(new { acknowledged = true });
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("reference_id", out var refProp) ||
                !Guid.TryParse(refProp.GetString(), out var kycId))
            {
                logger.LogWarning("Dojah webhook references an unknown kyc id.");
                return Ok(new { acknowledged = true });
            }

            // Only terminal statuses are applied; Ongoing/Pending get a follow-up event.
            var lifecycle = root.TryGetProperty("verification_status", out var vs)
                ? vs.GetString()?.Trim().ToLowerInvariant()
                : null;
            if (lifecycle is not ("completed" or "failed" or "abandoned"))
            {
                logger.LogInformation("Dojah session {Ref} not final, skipping: {Status}",
                    refProp.GetString(), lifecycle);
                return Ok(new { acknowledged = true });
            }

            // Completed means finished, not passed — check the step statuses.
            var passed = root.TryGetProperty("status", out var statusProp) &&
                statusProp.ValueKind == JsonValueKind.True;

            var providerRef = refProp.GetString()!;
            DateTimeOffset? completedAt = null;
            if (root.TryGetProperty("created_at", out var createdProp) &&
                DateTimeOffset.TryParse(createdProp.GetString(), out var created))
            {
                completedAt = created;
            }

            var cmd = new ApplyVerificationResultCommand(
                KycVerificationId: kycId,
                Passed: passed,
                Provider: "dojah",
                ProviderRef: providerRef,
                CompletedAt: completedAt);

            var outcome = await mediator.Send(cmd, ct);
            if (outcome.IsSuccess is false)
            {
                logger.LogWarning("Apply verification result failed for {KycId}: {Error}", kycId, outcome.Error);
            }

            return Ok(new { acknowledged = true });
        }
    }
}
