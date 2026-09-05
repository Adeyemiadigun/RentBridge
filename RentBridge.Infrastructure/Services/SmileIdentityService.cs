using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Infrastructure.Services;

/// <summary>
/// Smile ID server-to-server submitter. Gets a short-lived v3 token, then POSTs
/// a single biometric_kyc job (NIN + selfie + liveness frames) as multipart.
/// The result is delivered asynchronously to our webhook.
/// Base URL: sandbox https://testapi.smileidentity.com (default),
/// production https://api.smileidentity.com.
/// </summary>
public sealed class SmileIdentityService : IIdentityVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmileIdentityService> _logger;
    private readonly string _baseUrl;

    public SmileIdentityService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SmileIdentityService> logger)
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

    public async Task<Result<string?>> SubmitVerificationAsync(
        Guid kycId,
        NinNumber nin,
        VerificationSubject subject,
        IFormFile? selfieImage,
        IReadOnlyList<IFormFile> livenessImages,
        CancellationToken ct)
    {
        var openedStreams = new List<Stream>();
        try
        {
            var partnerId = _configuration["Smile:PartnerId"]
                ?? throw new InvalidOperationException("Smile:PartnerId is not configured.");
            var apiKey = _configuration["Smile:ApiKey"]
                ?? throw new InvalidOperationException("Smile:ApiKey is not configured.");

            if (selfieImage is null)
            {
                return Result<string?>.Fail("A selfie image is required to submit identity verification.");
            }

            var token = await GetTokenAsync(partnerId, apiKey, ct);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("NG"), "country");
            content.Add(new StringContent("NIN"), "id_type");
            content.Add(new StringContent(nin.Value), "id_number");
            content.Add(new StringContent(BuildConsentJson()), "consent");
            content.Add(new StringContent(BuildUserDetailsJson(subject)), "user_details");
            content.Add(new StringContent(
                JsonSerializer.Serialize(new { kyc_id = kycId.ToString("N") })), "partner_params");

            var callbackUrl = _configuration["Smile:CallbackUrl"];
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                content.Add(new StringContent(callbackUrl), "callback_url");
            }

            var selfieStream = selfieImage.OpenReadStream();
            openedStreams.Add(selfieStream);
            content.Add(new StreamContent(selfieStream), "selfie_image", selfieImage.FileName ?? "selfie.jpg");

            var frameIndex = 0;
            foreach (var frame in livenessImages)
            {
                var frameStream = frame.OpenReadStream();
                openedStreams.Add(frameStream);
                content.Add(new StreamContent(frameStream), "liveness_images", frame.FileName ?? $"liveness_{frameIndex}.jpg");
                frameIndex++;
            }

            _logger.LogInformation(
                "Submitting Smile biometric_kyc for kyc {KycId} with {LivenessCount} liveness frames",
                kycId, livenessImages.Count);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v3/biometric_kyc");
            request.Headers.Add("SmileID-Partner-ID", partnerId);
            request.Headers.Add("SmileID-Token", token);
            request.Content = content;

            using var response = await _httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Smile submit rejected ({Status}): {Body}",
                    (int)response.StatusCode, responseBody);
                return Result<string?>.Fail($"Smile ID verification submission failed: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var jobId = root.TryGetProperty("job_id", out var jobProp) && jobProp.ValueKind == JsonValueKind.String
                ? jobProp.GetString()
                : null;
            var message = root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                ? msgProp.GetString()
                : null;

            _logger.LogInformation("Smile submit accepted: job={JobId} ({Message})", jobId, message);
            return Result<string?>.Ok(jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting identity verification to Smile ID");
            return Result<string?>.Fail("An unexpected error occurred while submitting identity verification");
        }
        finally
        {
            foreach (var stream in openedStreams)
            {
                stream.Dispose();
            }
        }
    }

    private async Task<string> GetTokenAsync(string partnerId, string apiKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v3/token");
        request.Headers.Add("SmileID-Partner-ID", partnerId);
        request.Headers.Add("SmileID-API-Key", apiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Smile token request failed ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Smile token response did not contain a token.");
    }

    private string BuildConsentJson()
    {
        var privacyUrl = _configuration["Smile:PrivacyPolicyUrl"] ?? string.Empty;
        return JsonSerializer.Serialize(new
        {
            granted = true,
            granted_at = DateTimeOffset.UtcNow.ToString("O"),
            notice_language = "EN",
            notice_privacy_policy_url = privacyUrl,
        });
    }

    private static string BuildUserDetailsJson(VerificationSubject subject)
    {
        return JsonSerializer.Serialize(new
        {
            given_names = subject.GivenNames,
            last_name = subject.LastName,
            email = subject.Email,
            phone_number = subject.PhoneNumber,
        });
    }
}