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

    public async Task<Result<string>> CreateSdkSessionAsync(
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
                return Result<string>.Fail($"Smile ID token request failed: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var token = doc.RootElement.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String
                ? tokenProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<string>.Fail("Smile ID token response did not contain a token.");
            }

            return Result<string>.Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error requesting a Smile ID token");
            return Result<string>.Fail("An unexpected error occurred while requesting the verification token");
        }
    }
}
