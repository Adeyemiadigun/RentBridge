using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace RentBridge.Infrastructure.Verification;

/// <summary>
/// Verifies Dojah's HMAC SHA256 signature. Dojah signs the raw request body
/// with your secret key and sends it in the X-Dojah-Signature header.
/// </summary>
public sealed class DojahSignatureValidator : IDojahSignatureValidator
{
    private readonly byte[] _secret;

    public DojahSignatureValidator(IConfiguration configuration)
    {
        var secret = configuration["Dojah:WebhookSecret"]
            ?? throw new InvalidOperationException("Dojah:WebhookSecret is not configured.");
        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public bool IsValid(string rawBody, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(_secret);
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var expectedHex = Convert.ToHexString(expected);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHex),
            Encoding.UTF8.GetBytes(signature.Trim()));
    }
}
