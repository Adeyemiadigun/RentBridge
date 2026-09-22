namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// Verifies that an incoming webhook genuinely came from Dojah.
/// See https://docs.dojah.io/api-reference/core-concepts/webhooks-signatures
/// </summary>
public interface IDojahSignatureValidator
{
    /// <summary>
    /// Validates the x-dojah-signature header: HMAC-SHA256 of the raw
    /// request body, keyed with the secret key, hex-encoded.
    /// </summary>
    bool IsValidV1(string signature, byte[] rawBody);

    /// <summary>
    /// Validates the x-dojah-signature-v2 header: SHA256 of the secret
    /// key alone, hex-encoded. Works without access to the raw body.
    /// </summary>
    bool IsValidV2(string signature);
}
