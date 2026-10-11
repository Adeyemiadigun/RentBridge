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

    public Task<Result<string>> GetSignedDownloadUrlAsync(string fileUrl, CancellationToken ct)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.CloudName) || string.IsNullOrWhiteSpace(opts.ApiSecret))
        {
            return Task.FromResult(Result<string>.Fail("Cloudinary is not configured."));
        }

        try
        {
            var uri = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // URL format: https://res.cloudinary.com/{cloud}/{resource_type}/upload/{version?}/{public_id}.{format}
            int uploadIndex = Array.FindIndex(segments, s => s == "upload");
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
            {
                return Task.FromResult(Result<string>.Fail("Invalid Cloudinary URL format."));
            }

            var publicIdWithExt = string.Join("/", segments.Skip(uploadIndex + 1));
            var parts = publicIdWithExt.Split('/');

            // Drop a leading version segment (v<digits>) so the signed URL uses the
            // canonical form the signature is validated against.
            var first = parts[0];
            if (first.StartsWith('v') && first.Length > 1 && char.IsDigit(first[1]))
            {
                parts = parts.Skip(1).ToArray();
            }
            publicIdWithExt = string.Join("/", parts);

            if (string.IsNullOrWhiteSpace(publicIdWithExt))
            {
                return Task.FromResult(Result<string>.Fail("Invalid Cloudinary URL format."));
            }

            // Already signed; nothing to do.
            if (Array.Exists(segments, s => s.StartsWith("s--", StringComparison.Ordinal)))
            {
                return Task.FromResult(Result<string>.Ok(fileUrl));
            }

            // Delivery URL signature: SHA-1 of (everything after the signature
            // component + API secret), URL-safe base64, first 8 chars.
            var payload = $"{publicIdWithExt}{opts.ApiSecret}";
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(payload));
            var b64 = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var signature = b64[..Math.Min(8, b64.Length)];

            var signedUrl = $"{uri.Scheme}://{uri.Host}/{segments[0]}/{segments[1]}/upload/s--{signature}--/{publicIdWithExt}";
            return Task.FromResult(Result<string>.Ok(signedUrl));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<string>.Fail($"Failed to sign download URL: {ex.Message}"));
        }
    }

    public async Task<Result<StoredFile>> DownloadAsync(string fileUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Result<StoredFile>.Fail("A file URL is required.");
        }

        var signed = await GetSignedDownloadUrlAsync(fileUrl, ct);
        if (!signed.IsSuccess)
        {
            return Result<StoredFile>.Fail(signed.Error!);
        }

        try
        {
            using var response = await httpClient.GetAsync(signed.Value, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return Result<StoredFile>.Fail($"Download failed ({response.StatusCode}): {Truncate(body)}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType)
                || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                contentType = InferContentType(fileUrl) ?? contentType;
            }

            var fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
            return Result<StoredFile>.Ok(new StoredFile(
                bytes,
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                string.IsNullOrWhiteSpace(fileName) ? null : fileName));
        }
        catch (Exception ex)
        {
            return Result<StoredFile>.Fail($"Download failed: {ex.Message}");
        }
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

        // Resolve BOTH the resource type and the public id from the URL. The old
        // implementation always posted to raw/destroy and stripped the folder
        // (via Path.GetFileNameWithoutExtension), so it both failed for image
        // uploads and, for raw files whose public id keeps its extension and
        // folder, could target the wrong asset.
        if (!TryParseCloudinaryAsset(fileUrl, out var resourceType, out var publicId))
        {
            return Result.Fail("Could not extract the asset reference from the URL.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("timestamp", timestamp),
            new("invalidate", "true"),
            new("public_id", publicId),
        };

        var signed = string.Join("&",
            parameters.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
        var signature = Sha1Hex($"{signed}{opts.ApiSecret}");

        var deleteUrl = $"https://api.cloudinary.com/v1_1/{opts.CloudName}/{resourceType}/destroy";
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
            if (doc.RootElement.TryGetProperty("result", out var resultElement))
            {
                var resultValue = resultElement.GetString();
                // "not found" means the asset is already gone — treat as success so
                // a retried cleanup does not loop forever on a missing file.
                return resultValue is "ok" or "not found"
                    ? Result.Ok()
                    : Result.Fail($"Delete failed: {Truncate(body)}");
            }
        }
        catch (JsonException)
        {
            // Non-JSON 2xx response: fall through and accept it.
        }

        return Result.Ok();
    }

    /// <summary>
    /// Extracts the Cloudinary resource type and public id from a delivery URL of
    /// the form .../{resource}/upload/[s--sig--/][v{version}/]{public_id}. For
    /// image/video the delivery extension is dropped from the public id; for raw
    /// files the extension is part of the public id and is kept.
    /// </summary>
    private static bool TryParseCloudinaryAsset(string fileUrl, out string resourceType, out string publicId)
    {
        resourceType = string.Empty;
        publicId = string.Empty;

        try
        {
            var uri = new Uri(fileUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 4)
            {
                return false;
            }

            var resource = segments[1];
            if (resource is not ("image" or "raw" or "video"))
            {
                return false;
            }

            var uploadIndex = Array.FindIndex(segments, s => s.Equals("upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
            {
                return false;
            }

            var rest = segments.Skip(uploadIndex + 1).ToList();

            if (rest.Count > 0 && rest[0].StartsWith("s--", StringComparison.Ordinal))
            {
                rest.RemoveAt(0);
            }

            if (rest.Count > 0 && rest[0].Length > 1 && rest[0][0] == 'v' && char.IsDigit(rest[0][1]))
            {
                rest.RemoveAt(0);
            }

            if (rest.Count == 0)
            {
                return false;
            }

            var id = string.Join("/", rest);
            if (resource is "image" or "video")
            {
                id = Path.ChangeExtension(id, null);
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            resourceType = resource;
            publicId = id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? InferContentType(string fileUrl)
    {
        string ext;
        try
        {
            ext = Path.GetExtension(new Uri(fileUrl).AbsolutePath).ToLowerInvariant();
        }
        catch
        {
            ext = Path.GetExtension(fileUrl).ToLowerInvariant();
        }

        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => null,
        };
    }

    private static string Sha1Hex(string input)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int max = 300)
        => value.Length <= max ? value : value[..max];
}