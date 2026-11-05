using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Common.Interfaces;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Hosts user files (listing photos, ownership documents) to object storage
/// and returns the hosted URL for the caller to attach to a property/listing.
/// Accepts images (JPEG/PNG/WebP) and PDFs only, capped at 500 KB.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
[Produces("application/json")]
public sealed class UploadController(IFileStorage storage) : ControllerBase
{
    /// <summary>Hard ceiling on uploaded file payloads.</summary>
    private const int MaxFileBytes = 500 * 1024;

    // Extension, declared content type, and magic bytes must all agree, so a
    // renamed .exe or an HTML payload can't ride in on a mismatched header.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf",
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/pjpeg", "image/png", "image/webp", "application/pdf",
    };

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file selected." });
        }

        if (file.Length > MaxFileBytes)
        {
            return BadRequest(new { error = $"File exceeds the {MaxFileBytes / 1024} KB upload limit." });
        }

        if (!IsAllowed(file.FileName, file.ContentType))
        {
            return BadRequest(new { error = "Allowed file types: JPG, PNG, WebP, PDF." });
        }

        // Snapshot the header for magic-byte checks, then rewind so the whole
        // stream goes to storage.
        await using var stream = file.OpenReadStream();
        var header = new byte[Math.Min(file.Length, 12)];
        stream.ReadExactly(header);
        stream.Position = 0;

        if (!IsAllowedHeader(header))
        {
            return BadRequest(new { error = "File contents do not match an allowed file type." });
        }

        var result = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { url = result.Value });
    }

    private static bool IsAllowed(string fileName, string contentType) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName)) &&
        AllowedContentTypes.Contains(contentType);

    private static bool IsAllowedHeader(ReadOnlySpan<byte> header) =>
        HasJpegHeader(header) || HasPngHeader(header) || HasWebPHeader(header) || HasPdfHeader(header);

    private static bool HasJpegHeader(ReadOnlySpan<byte> b) =>
        b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    private static bool HasPngHeader(ReadOnlySpan<byte> b) =>
        b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;

    private static bool HasWebPHeader(ReadOnlySpan<byte> b) =>
        b.Length >= 12 && b[0..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8);

    private static bool HasPdfHeader(ReadOnlySpan<byte> b) =>
        b.Length >= 5 && b[0..5].SequenceEqual("%PDF-"u8);
}
