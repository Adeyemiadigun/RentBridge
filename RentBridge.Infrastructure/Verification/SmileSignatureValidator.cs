using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace RentBridge.Infrastructure.Verification;

/// <summary>
/// Verifies Smile ID's webhook signature. Smile signs the concatenation of
/// the raw "Response-Timestamp" header + the partner ID + "sid_request"
/// with your API key (HMAC-SHA256) and Base64-encodes it into the
/// "Response-Signature" header.
/// </summary>
public sealed class SmileSignatureValidator : ISmileSignatureValidator
{
    private readonly string _partnerId;
    private readonly byte[] _apiKey;

    public SmileSignatureValidator(IConfiguration configuration)
    {
        _partnerId = configuration["Smile:PartnerId"]
            ?? throw new InvalidOperationException("Smile:PartnerId is not configured.");
        _apiKey = Encoding.UTF8.GetBytes(configuration["Smile:ApiKey"]
            ?? throw new InvalidOperationException("Smile:ApiKey is not configured."));
    }

    public bool IsValid(string signature, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
        {
            return false;
        }

        var payload = timestamp + _partnerId + "sid_request";
        using var hmac = new HMACSHA256(_apiKey);
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim()));
    }
}