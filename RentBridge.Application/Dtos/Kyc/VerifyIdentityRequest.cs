namespace RentBridge.Application.Dtos.Kyc
{
    /// <summary>
    /// Starts a verification with the active provider (default Dojah).
    /// The frontend opens the vendor widget with the returned session and
    /// pre-fills gov_data.nin itself. No images are sent to this endpoint.
    /// </summary>
    public sealed record VerifyIdentityRequest
    {
        /// <summary>11-digit National Identification Number.</summary>
        /// <example>70123456789</example>
        public string Nin { get; init; } = string.Empty;
    }
}
