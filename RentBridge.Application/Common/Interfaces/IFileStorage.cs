using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Uploads a file to object storage and returns the hosted HTTP(S) URL.
/// Implemented by Cloudinary today; swap in S3/R2 by adding another
/// implementation and registering it in place.
/// </summary>
public interface IFileStorage
{
    Task<Result<string>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct);
    Task<Result> DeleteAsync(string fileUrl, CancellationToken ct);

    /// <summary>
    /// Returns a Cloudinary signed delivery URL for an already-uploaded asset.
    /// Raw/PDF delivery (and private assets) are gated on the signed URL
    /// component /s--SIGNATURE--/, so a browser fetch of the plain URL fails
    /// with 401 "deny or ACL failure". The signature must be generated
    /// server-side because it is derived from the API secret.
    /// </summary>
    Task<Result<string>> GetSignedDownloadUrlAsync(string fileUrl, CancellationToken ct);

    /// <summary>
    /// Fetches the bytes of an already-uploaded asset, server-side. Used to
    /// proxy the agreement document back to the browser so it can be rendered
    /// inline: Cloudinary serves raw files with
    /// <c>Content-Disposition: attachment</c> regardless of the requesting URL,
    /// so a direct link always forces a download. Streaming through the API lets
    /// the endpoint choose the disposition and keep the storage URL private.
    /// </summary>
    Task<Result<StoredFile>> DownloadAsync(string fileUrl, CancellationToken ct);
}

/// <summary>The retrieved bytes of a stored file plus its resolved content type.</summary>
public sealed record StoredFile(byte[] Content, string ContentType, string? FileName);