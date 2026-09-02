using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.ApplyVerificationResult;
using RentBridge.Infrastructure.Verification;

namespace RentBridge.Api.Controllers;

[ApiController]
[Route("api/webhooks/dojah")]
public class DojahWebhookController(
    IMediator mediator,
    IDojahSignatureValidator signatureValidator,
    ILogger<DojahWebhookController> logger) : ControllerBase
{
    
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        // 1. Read the raw body once - needed both for signature verification
        //    and payload deserialization (the model binder would have consumed it).
        var rawBody = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        // 2. Verify the HMAC signature so only genuine Dojah calls are trusted.
        if (!Request.Headers.TryGetValue("X-Dojah-Signature", out var signature))
        {
            logger.LogWarning("Dojah webhook missing X-Dojah-Signature header.");
            return Ok(new { acknowledged = true });
        }

        if (!signatureValidator.IsValid(rawBody, signature.ToString()))
        {
            logger.LogWarning("Dojah webhook signature verification failed.");
            return Ok(new { acknowledged = true });
        }

        // 3. Deserialize the payload. Our DTO is tolerant of the real shape.
        DojahWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DojahWebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Dojah webhook payload could not be deserialized.");
            return Ok(new { acknowledged = true });
        }

        if (payload is null)
        {
            return Ok(new { acknowledged = true });
        }

        // 4. Correlate Dojah's reference back to OUR KycVerification id.
        //    The submit call passes our kyc id as the reference_id, so this
        //    round-trips cleanly. If it isn't a Guid, we can't process it.
        if (!Guid.TryParse(payload.ReferenceId, out var kycId))
        {
            logger.LogWarning("Dojah webhook reference_id is not a valid kyc id: {Ref}", payload.ReferenceId);
            return Ok(new { acknowledged = true });
        }

        // 5. 'Completed' only means the session finished - it does NOT mean passed.
        //    We inspect the per-step statuses below.
        if (payload.VerificationStatus is not null &&
            !string.Equals(payload.VerificationStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Dojah session did not complete: {Status}", payload.VerificationStatus);
            return Ok(new { acknowledged = true });
        }

        var providerRef = payload.ReferenceId;
        var completedAt = DateTimeOffset.UtcNow;

        // 6. Dispatch one Apply command per verification half that the flow ran.
        //    Dojah nests per-step outcomes under the event; we map the NIN identity
        //    and the selfie (facial) check. Missing/malformed steps are skipped.
        var results = new List<(string Kind, bool Passed)>();

        var government = payload.Data?.GovernmentData;
        if (government is not null)
        {
            results.Add(("identity", government.Status == true));
        }

        var selfie = payload.Data?.SelfieData ?? payload.Data?.FaceMatchData;
        if (selfie is not null)
        {
            results.Add(("facial", selfie.Status == true));
        }

        foreach (var (kind, passed) in results)
        {
            var cmd = new ApplyVerificationResultCommand(
                KycVerificationId: kycId,
                Kind: kind,
                Passed: passed,
                Provider: "dojah",
                ProviderRef: providerRef,
                CompletedAt: completedAt);

            var outcome = await mediator.Send(cmd, ct);
            if (outcome.IsSuccess is false)
            {
                logger.LogWarning("Apply {Kind} failed for {KycId}: {Error}", kind, kycId, outcome.Error);
            }
        }

        return Ok(new { acknowledged = true });
    }
}
