namespace RentBridge.Infrastructure.Verification;

/// <summary>Minimal, tolerant shape of a Dojah webhook event payload.</summary>
public sealed class DojahWebhookPayload
{
    public string? ReferenceId { get; set; }
    public string? VerificationStatus { get; set; }
    public string? Status { get; set; }
    public DataSection? Data { get; set; }
}

public sealed class DataSection
{
    public StepStatus? GovernmentData { get; set; }
    public StepStatus? SelfieData { get; set; }
    public StepStatus? FaceMatchData { get; set; }
}

public sealed class StepStatus
{
    public bool? Status { get; set; }
}
