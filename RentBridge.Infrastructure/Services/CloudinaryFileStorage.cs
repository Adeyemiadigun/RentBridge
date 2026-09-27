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

        // Make raw uploads (documents/PDFs) publicly accessible
        if (resourceType == "raw")
        {
            parameters.Add(new("access_mode", "public"));
        }

        var signed = string.Join("&",
            parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
        var signature = Sha1Hex($"{signed}{opts.ApiSecret}");

        // DEBUG: Log the parameters being sent
        Console.WriteLine($"[CLOUDINARY DEBUG] Upload parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
        Console.WriteLine($"[CLOUDINARY DEBUG] Resource type: {resourceType}");
        Console.WriteLine($"[CLOUDINARY DEBUG] Signed string: {signed}");

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

        // DEBUG: Log Cloudinary response
        Console.WriteLine($"[CLOUDINARY DEBUG] Response status: {response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync(ct);
        Console.WriteLine($"[CLOUDINARY DEBUG] Response body: {body}");
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
                Console.WriteLine($"[CLOUDINARY DEBUG] Secure URL: {url}");
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

    public async Task<Result> DeleteAsync(string fileUrl, CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.CloudName)
            || string.IsNullOrWhiteSpace(opts.ApiKey)
            || string.IsNullOrWhiteSpace(opts.ApiSecret))
        {
            return Result.Fail("Cloudinary is not configured.");
        }

        // Extract public_id from the Cloudinary URL
        // URL format: https://res.cloudinary.com/{cloud_name}/{resource_type}/upload/{version}/{public_id}.{format}
        // Or: https://res.cloudinary.com/{cloud_name}/raw/upload/{version}/{public_id}.{format}
        try
        {
            var uri = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/');
            // Find the upload segment and get everything after it
            int uploadIndex = Array.FindIndex(segments, s => s == "upload" || s == "raw" || s == "image" || s == "video");
            
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
            {
                return Result.Fail("Could not extract public_id from URL - invalid URL format");
            }
            
            // Get everything after "upload/" as the public_id (without extension)
            var publicIdParts = segments.Skip(uploadIndex + 1).ToArray();
            var publicIdWithExt = string.Join("/", publicIdParts);
            // Remove file extension
            var publicId = Path.GetFileNameWithoutExtension(publicIdWithExt);
            
            // If there's a version number (starts with v), remove it
            if (publicId.StartsWith("v") && publicId.Length > 1 && char.IsDigit(publicId[1]))
            {
                var versionEnd = publicId.IndexOf('/');
                if (versionEnd > 0)
                {
                    publicId = publicId.Substring(versionEnd + 1);
                }
            }

            if (string.IsNullOrWhiteSpace(publicId))
            {
                return Result.Fail("Could not extract public_id from URL");
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("timestamp", timestamp),
                new("invalidate", "true"),
            };

            var signed = string.Join("&",
                parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={p.Value}"));
            var signature = Sha1Hex($"{signed}{options.Value.ApiSecret}");

            var deleteUrl = $"https://api.cloudinary.com/v1_1/{options.Value.CloudName}/raw/destroy";
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(opts.ApiKey), "api_key");
            content.Add(new StringContent(publicId), "public_id");
            content.Add(new StringContent(signature), "signature");
            content.Add(new StringContent(timestamp), "timestamp");
            content.Add(new StringContent("true"), "invalidate");

            var response = await httpClient.PostAsync(deleteUrl, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail($"Delete failed ({response.StatusCode}): {Truncate(body)}");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("result", out var resultElement) &&
                    resultElement.GetString() == "ok")
                {
                    return Result.Ok();
                }
            }
            catch (JsonException)
            {
                // Ignore parse errors, check status code
            }

            if (response.IsSuccessStatusCode)
            {
                return Result.Ok();
            }
            
            return Result.Fail($"Delete failed: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Delete failed: {ex.Message}");
        }
    }

    private static string Sha1Hex(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<Result<string>> GetSignedUrlAsync(string publicId, string resourceType, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.CloudName)
            || string.IsNullOrWhiteSpace(opts.ApiKey)
            || string.IsNullOrWhiteSpace(opts.ApiSecret))
        {
            return Result<string>.Fail("Cloudinary is not configured.");
        }

        var expirySeconds = (int)(expiration ?? TimeSpan.FromHours(1)).TotalSeconds;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expirySeconds).ToUnixTimeSeconds().ToString();

        // Cloudinary signed URL signature must include public_id
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("public_id", publicId),
            new("timestamp", timestamp),
            new("expires_at", expiresAt),
        };

        var signed = string.Join("&",
            parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
        var signature = Sha1Hex($"{signed}{opts.ApiSecret}");

        // Cloudinary signed URL format: /s--{signature}--/{transformations}/{public_id}
        // We use version timestamp as transformation
        var url = $"https://res.cloudinary.com/{opts.CloudName}/{resourceType}/upload/s--{signature}--/v{timestamp}/{publicId}";

        return Result<string>.Ok(url);
    }

    private static string Truncate(string value, int max = 300)
        => value.Length <= max ? value : value[..max];
}