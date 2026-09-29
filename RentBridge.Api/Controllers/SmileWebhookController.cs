using System.Text.Json;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.ApplyVerificationResult;
using RentBridge.Infrastructure.Verification;

namespace RentBridge.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[Route("api/webhooks/smile")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
public class SmileWebhookController(
    IMediator mediator,
    ISmileSignatureValidator signatureValidator,
    ILogger<SmileWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        // 1. Read the raw body once - the signature is over headers only, but we
        //    still need it for deserialization (the model binder would have consumed it).
        var rawBody = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        // 2. Smile signs "Response-Timestamp + partnerId + sid_request" with the
        //    API key and sends it in the Response-Signature header.
        if (!Request.Headers.TryGetValue("Response-Signature", out var signature) ||
            !Request.Headers.TryGetValue("Response-Timestamp", out var timestamp))
        {
            logger.LogWarning("Smile webhook missing Response-Signature/Response-Timestamp headers.");
            return Ok(new { acknowledged = true });
        }

        if (!signatureValidator.IsValid(signature.ToString(), timestamp.ToString()))
        {
            logger.LogWarning("Smile webhook signature verification failed.");
            return Ok(new { acknowledged = true });
        }

        // 3. Deserialize the payload. Our DTO is tolerant of the real shape.
        SmileWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SmileWebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Smile webhook payload could not be deserialized.");
            return Ok(new { acknowledged = true });
        }

        if (payload is null)
        {
            return Ok(new { acknowledged = true });
        }

        // 4. Correlate back to OUR KycVerification id. The submit call passes our
        //    kyc id as Smile's user_id, which Smile echoes in the User-ID header;
        //    we also embed it in partner_params.kyc_id as a fallback.
        Request.Headers.TryGetValue("User-ID", out var userIdHeader);
        if (ResolveKycId(userIdHeader.ToString(), payload) is not Guid kycId)
        {
            logger.LogWarning("Smile webhook references an unknown kyc id.");
            return Ok(new { acknowledged = true });
        }

        // 5. Only final verdicts are applied. "processing" (awaiting human review)
        //    and any unrecognized status are acknowledged and skipped - a final
        //    webhook follows. "attention" is not a verdict either; treat conservatively.
        var passed = payload.Status?.Trim().ToLowerInvariant() switch
        {
            "clear" => true,
            "block" or "error" => false,
            _ => (bool?)null,
        };

        if (passed is null)
        {
            logger.LogInformation("Smile job {JobId} not final, skipping: {Status}", payload.JobId, payload.Status);
            return Ok(new { acknowledged = true });
        }

        // 6. Provider reference: Smile exposes the job id via the Job-ID header;
        //    otherwise fall back to the payload or the user id we sent.
        Request.Headers.TryGetValue("Job-ID", out var jobHeader);
        var providerRef = string.IsNullOrWhiteSpace(jobHeader.ToString())
            ? (string.IsNullOrWhiteSpace(payload.JobId)
                ? userIdHeader.ToString()
                : payload.JobId)
            : jobHeader.ToString();

        if (string.IsNullOrWhiteSpace(providerRef))
        {
            providerRef = payload.CompletedAt?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O");
        }

        var cmd = new ApplyVerificationResultCommand(
            KycVerificationId: kycId,
            Passed: passed.Value,
            Provider: "smile",
            ProviderRef: providerRef,
            CompletedAt: payload.CompletedAt);

        var outcome = await mediator.Send(cmd, ct);
        if (outcome.IsSuccess is false)
        {
            logger.LogWarning("Apply verification result failed for {KycId}: {Error}", kycId, outcome.Error);
        }

        return Ok(new { acknowledged = true });
    }

    private static Guid? ResolveKycId(string userIdHeader, SmileWebhookPayload payload)
    {
        if (!string.IsNullOrWhiteSpace(userIdHeader) && Guid.TryParse(userIdHeader, out var fromHeader))
        {
            return fromHeader;
        }

        if (payload.PartnerParams is JsonElement partnerParams &&
            partnerParams.TryGetProperty("kyc_id", out var kycProp) &&
            Guid.TryParse(kycProp.GetString(), out var fromParams))
        {
            return fromParams;
        }

        return null;
    }
}