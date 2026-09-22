using System.Text.Json;

namespace RentBridge.Infrastructure.Verification;

/// <summary>Minimal, tolerant shape of a Smile ID verification webhook payload.</summary>
public sealed class SmileWebhookPayload
{
    /// <summary>Overall disposition: "clear", "block", "error", "attention", "processing".</summary>
    public string? Status { get; set; }

    public string? Message { get; set; }

    /// <summary>Machine-readable code for the specific cause of the status.</summary>
    public string? Reason { get; set; }

    public string? Product { get; set; }

    public string? JobId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Partner-provided metadata echoed back from the submit request.</summary>
    public JsonElement? PartnerParams { get; set; }
}