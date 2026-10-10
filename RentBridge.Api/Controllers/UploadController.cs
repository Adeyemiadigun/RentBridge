using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Common.Interfaces;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Hosts user files (listing photos, ownership documents) to object storage
/// and returns the hosted URL for the caller to attach to a property/listing.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
[Produces("application/json")]
public sealed class UploadController(IFileStorage storage) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file selected." });
        }

        await using var stream = file.OpenReadStream();
        var result = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);
        if (result.IsSuccess is false)
        {
            return result.ToErrorResponse();
        }

        return Ok(new { url = result.Value });
    }

    /// <summary>
    /// Returns a short-(validity)-signed Cloudinary delivery URL for a stored
    /// file so the browser can download it. Raw/PDF delivery (and private
    /// assets) requires the signed /s--SIGNATURE--/ component, which can only
    /// be generated server-side from the API secret.
    /// </summary>
    [HttpGet("signed-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSignedUrl([FromQuery] string fileUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)
            || !Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
            || !uri.Host.EndsWith("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "A valid res.cloudinary.com fileUrl is required." });
        }

        var result = await storage.GetSignedDownloadUrlAsync(fileUrl, ct);
        if (result.IsSuccess is false)
        {
            return result.ToErrorResponse();
        }

        return Ok(new { url = result.Value });
    }
}