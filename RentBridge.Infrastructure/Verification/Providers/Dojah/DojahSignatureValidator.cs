using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// HMAC-SHA256 webhook signature checks per Dojah docs.
/// Uses Dojah:WebhookSecret when configured (dashboard webhook key),
/// otherwise falls back to Dojah:SecretKey.
/// </summary>
public sealed class DojahSignatureValidator : IDojahSignatureValidator
{
    private readonly byte[] _secretBytes;
    private readonly string _secretV2Hash;

    public DojahSignatureValidator(IConfiguration configuration, IOptions<DojahOptions> options)
    {
        var secret = !string.IsNullOrWhiteSpace(options.Value.WebhookSecret)
            ? options.Value.WebhookSecret
            : !string.IsNullOrWhiteSpace(options.Value.ResolvedSecretKey)
                ? options.Value.ResolvedSecretKey
                : configuration["Dojah:WebhookSecret"]
                    ?? configuration["Dojah:SecretKey"]
                    ?? configuration["Dojah:ApiKey"]
                    ?? throw new InvalidOperationException("Dojah:WebhookSecret is not configured.");

        _secretBytes = Encoding.UTF8.GetBytes(secret);
        _secretV2Hash = Convert.ToHexString(SHA256.HashData(_secretBytes)).ToLowerInvariant();
    }

    public bool IsValidV1(string signature, byte[] rawBody)
    {
        if (string.IsNullOrWhiteSpace(signature) || rawBody is null || rawBody.Length == 0)
        {
            return false;
        }

        using var hmac = new HMACSHA256(_secretBytes);
        var expected = Convert.ToHexString(hmac.ComputeHash(rawBody)).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    public bool IsValidV2(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(_secretV2Hash),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }
}
