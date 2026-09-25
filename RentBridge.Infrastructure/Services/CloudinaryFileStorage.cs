using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Domain.Common;

namespace RentBridge.Infrastructure.Services;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

/// <summary>
/// Uploads photos and ownership documents to Cloudinary and returns the
/// secure hosted URL, which callers store on the property/listing.
/// Uses Cloudinary's REST upload API with a signed (timestamp + SHA-1)
/// signature. Registered as a typed HttpClient client, so the injected
/// client is a shared, configured HttpClient for outbound calls.
/// </summary>
public sealed class CloudinaryFileStorage(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options) : IFileStorage
{
    private const string Folder = "rentbridge";

    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.CloudName)
            || string.IsNullOrWhiteSpace(opts.ApiKey)
            || string.IsNullOrWhiteSpace(opts.ApiSecret))
        {
            return Result<string>.Fail("File upload is not configured. Set Cloudinary credentials to enable uploads.");
        }

        var resourceType = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? "image"
            : "raw";

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("folder", Folder),
            new("timestamp", timestamp),
        };

        var signed = string.Join("&",
            parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
        var signature = Sha1Hex($"{signed}{opts.ApiSecret}");

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(opts.ApiKey), "api_key");
        content.Add(new StringContent(signature), "signature");
        foreach (var p in parameters)
        {
            content.Add(new StringContent(p.Value), p.Key);
        }

        var client = httpClient;
        client.DefaultRequestHeaders.Clear();
        var response = await client.PostAsync(
            $"https://api.cloudinary.com/v1_1/{opts.CloudName}/{resourceType}/upload",
            content,
            ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            return Result<string>.Fail($"Upload failed ({response.StatusCode}): {Truncate(body)}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("secure_url", out var urlElement) &&
                urlElement.ValueKind == JsonValueKind.String)
            {
                var url = urlElement.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return Result<string>.Ok(url);
                }
            }
        }
        catch (JsonException)
        {
            return Result<string>.Fail("Upload failed: invalid response from storage provider.");
        }

        return Result<string>.Fail("Upload returned no URL.");
    }

    private static string Sha1Hex(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int max = 300)
        => value.Length <= max ? value : value[..max];
}