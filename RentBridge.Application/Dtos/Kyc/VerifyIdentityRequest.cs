namespace RentBridge.Application.Dtos.Kyc
{
    /// <summary>
    /// Starts a verification with the active provider (default Dojah).
    /// SelfieImage is the base64 selfie (data:...;base64, prefix optional).
    /// </summary>
    public sealed record VerifyIdentityRequest
    {
        public string Nin { get; init; } = string.Empty;
        public string? SelfieImage { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
    }
}
