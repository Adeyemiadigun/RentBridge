using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Domain.Common;

namespace RentBridge.Infrastructure.Verification.Providers.Dojah;

/// <summary>
/// Default verification strategy: Dojah NIN + selfie face-match.
/// POST {base}/api/v1/kyc/nin/verify with {nin, selfie_image, threshold?}.
/// See https://docs.dojah.io/api-reference/individual-verification/nigeria/bvn-nin-selfie
/// </summary>
public sealed class DojahIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "dojah";
    public bool SupportsSyncVerification => true;
    public bool SupportsSdkSession => false;

    private readonly HttpClient _httpClient;
    private readonly DojahOptions _options;
    private readonly ILogger<DojahIdentityVerificationProvider> _logger;

    public DojahIdentityVerificationProvider(
        HttpClient httpClient,
        IOptions<DojahOptions> options,
        ILogger<DojahIdentityVerificationProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<Result<string>> CreateSdkSessionAsync(
        Guid kycVerificationId, string nin, CancellationToken ct) =>
        Task.FromResult(Result<string>.Fail(
            "Dojah verifies NIN+selfie server-side; no SDK session to mint. " +
            "Call VerifyAsync with the selfie image instead."));

    public async Task<Result<ProviderVerificationOutcome>> VerifyAsync(
        IdentityVerificationRequest request, CancellationToken ct)
    {
        try
        {
            var secret = _options.ResolvedSecretKey;
            if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(secret))
            {
                return Result<ProviderVerificationOutcome>.Fail(
                    "Dojah:AppId / Dojah:SecretKey is not configured.");
            }

            var selfie = StripDataUriPrefix(request.SelfieImageBase64);
            if (string.IsNullOrWhiteSpace(selfie))
            {
                return Result<ProviderVerificationOutcome>.Fail(
                    "A selfie image is required for Dojah verification.");
            }

            try
            {
                _ = Convert.FromBase64String(selfie);
            }
            catch (FormatException)
            {
                return Result<ProviderVerificationOutcome>.Fail(
                    "Selfie image is not valid base64.");
            }

            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["nin"] = request.Nin,
                ["selfie_image"] = selfie,
            };
            if (_options.Threshold is >= 50 and <= 100)
            {
                payload["threshold"] = _options.Threshold;
            }
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                payload["first_name"] = request.FirstName;
            }
            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                payload["last_name"] = request.LastName;
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post, $"{_options.ResolvedBaseUrl}/api/v1/kyc/nin/verify");
            // Raw secret — Dojah rejects the "Bearer" prefix with 401.
            httpRequest.Headers.Add("Authorization", secret);
            httpRequest.Headers.Add("AppId", _options.AppId);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                return status switch
                {
                    400 => Result<ProviderVerificationOutcome>.Fail(
                        "Dojah rejected the verification request (missing field or malformed image)."),
                    401 => Result<ProviderVerificationOutcome>.Fail(
                        "Dojah authentication failed. Check Dojah:AppId and Dojah:SecretKey."),
                    402 => Result<ProviderVerificationOutcome>.Fail(
                        "Dojah wallet has insufficient balance. Fund the wallet and retry."),
                    404 => Result<ProviderVerificationOutcome>.Fail(
                        "No NIMC record found for this NIN."),
                    424 => Result<ProviderVerificationOutcome>.Fail(
                        "Dojah upstream source is temporarily unavailable. Retry shortly."),
                    429 => Result<ProviderVerificationOutcome>.Fail(
                        "Too many verification requests. Back off and retry."),
                    _ => FailWithLog(status, body),
                };
            }

            return ParseSuccess(request.Nin, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Dojah verification");
            return Result<ProviderVerificationOutcome>.Fail(
                "An unexpected error occurred during identity verification.");
        }
    }

    private Result<ProviderVerificationOutcome> FailWithLog(int status, string body)
    {
        _logger.LogWarning("Dojah verification failed ({Status}): {Body}", status, body);
        return Result<ProviderVerificationOutcome>.Fail(
            $"Dojah verification failed (HTTP {status}).");
    }

    private static Result<ProviderVerificationOutcome> ParseSuccess(string nin, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("entity", out var entity) ||
                entity.ValueKind != JsonValueKind.Object)
            {
                return Result<ProviderVerificationOutcome>.Fail(
                    "Dojah response did not contain an identity record.");
            }

            var passed = false;
            var confidence = 0d;
            if (entity.TryGetProperty("selfie_verification", out var selfieVerification) &&
                selfieVerification.ValueKind == JsonValueKind.Object)
            {
                if (selfieVerification.TryGetProperty("match", out var match))
                {
                    passed = match.ValueKind == JsonValueKind.True;
                }
                if (selfieVerification.TryGetProperty("confidence_value", out var score) &&
                    score.ValueKind == JsonValueKind.Number &&
                    score.TryGetDouble(out var value))
                {
                    confidence = value;
                }
            }

            // Dojah's sync API returns no job/reference id; use a deterministic ref.
            return Result<ProviderVerificationOutcome>.Ok(new ProviderVerificationOutcome(
                passed, confidence, $"nin:{nin}", DateTimeOffset.UtcNow));
        }
        catch (JsonException)
        {
            return Result<ProviderVerificationOutcome>.Fail(
                "Dojah response could not be understood.");
        }
    }

    /// <summary>Dojah requires the raw buffer — strip any data:...;base64, prefix.</summary>
    internal static string? StripDataUriPrefix(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();
        var comma = trimmed.IndexOf(',');
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
        {
            return trimmed[(comma + 1)..].Trim();
        }

        return trimmed;
    }
}
