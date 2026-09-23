namespace RentBridge.Application.Dtos.Kyc
{
    /// <summary>
    /// The current user's latest KYC state. Status is one of
    /// "none" (never started), "pending" (widget opened, awaiting webhook
    /// verdict), "verified", or "rejected". Poll this after the widget's
    /// onSuccess — the client callback alone never proves a pass.
    /// </summary>
    public sealed record VerificationStatusResponse(
        Guid? KycId,
        string Provider,
        string Status,
        DateTimeOffset? CompletedAt);
}
