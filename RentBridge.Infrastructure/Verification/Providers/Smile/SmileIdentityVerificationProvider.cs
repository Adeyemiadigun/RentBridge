using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Domain.Common;

namespace RentBridge.Infrastructure.Verification.Providers.Smile;

/// <summary>
/// Legacy strategy: Smile ID client-SDK flow. The backend mints a
/// short-lived v3 JWT; the device submits the biometric_kyc job directly;
/// the verdict arrives on the Smile webhook.
/// Base URL: sandbox https://testapi.smileidentity.com (default),
/// production https://api.smileidentity.com.
/// </summary>
public sealed class SmileIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "smile";
    public bool SupportsSyncVerification => false;
    public bool SupportsSdkSession => true;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmileIdentityVerificationProvider> _logger;
    private readonly string _baseUrl;

    public SmileIdentityVerificationProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SmileIdentityVerificationProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(configuration["Smile:BaseUrl"]))
        {
            _baseUrl = configuration["Smile:BaseUrl"]!.TrimEnd('/');
        }
        else
        {
            var environment = configuration["Smile:Environment"]?.Trim().ToLowerInvariant();
            _baseUrl = environment == "production"
                ? "https://api.smileidentity.com"
                : "https://testapi.smileidentity.com";
        }
    }

    public Task<Result<ProviderVerificationOutcome>> VerifyAsync(
        IdentityVerificationRequest request, CancellationToken ct) =>
        Task.FromResult(Result<ProviderVerificationOutcome>.Fail(
            "Smile ID uses the client SDK flow; no synchronous verification available. " +
            "Call CreateSdkSessionAsync and submit the biometric_kyc job from the device."));

    public async Task<Result<SdkSession>> CreateSdkSessionAsync(
        Guid kycVerificationId, string nin, CancellationToken ct)
    {
        try
        {
            var partnerId = _configuration["Smile:PartnerId"]
                ?? throw new InvalidOperationException("Smile:PartnerId is not configured.");
            var apiKey = _configuration["Smile:ApiKey"]
                ?? throw new InvalidOperationException("Smile:ApiKey is not configured.");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v3/token");
            request.Headers.Add("SmileID-Partner-ID", partnerId);
            request.Headers.Add("SmileID-API-Key", apiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Smile token request failed ({Status}): {Body}",
                    (int)response.StatusCode, responseBody);
                return Result<SdkSession>.Fail($"Smile ID token request failed: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var token = doc.RootElement.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String
                ? tokenProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<SdkSession>.Fail("Smile ID token response did not contain a token.");
            }

            var environment = _configuration["Smile:Environment"]?.Trim().ToLowerInvariant() == "production"
                ? "production"
                : "sandbox";

            return Result<SdkSession>.Ok(new SdkSession(
                kycVerificationId.ToString("D"),
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["token"] = token,
                    ["partnerId"] = partnerId,
                    ["environment"] = environment,
                    ["country"] = "NG",
                    ["idType"] = "NIN",
                    ["idNumber"] = nin,
                    ["callbackUrl"] = _configuration["Smile:CallbackUrl"],
                    ["privacyPolicyUrl"] = _configuration["Smile:PrivacyPolicyUrl"],
                    ["productType"] = "biometric_kyc",
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error requesting a Smile ID token");
            return Result<SdkSession>.Fail("An unexpected error occurred while requesting the verification token");
        }
    }
}
