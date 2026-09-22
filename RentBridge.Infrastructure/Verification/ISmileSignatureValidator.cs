namespace RentBridge.Infrastructure.Verification;

/// <summary>Verifies that an incoming webhook genuinely came from Smile ID.</summary>
public interface ISmileSignatureValidator
{
    /// <summary>
    /// Validates a Smile webhook signature over the raw header values.
    /// Smile signs "Response-Timestamp + partnerId + 'sid_request'"
    /// with your API key using HMAC-SHA256 and Base64-encodes the result.
    /// </summary>
    bool IsValid(string signature, string timestamp);
}