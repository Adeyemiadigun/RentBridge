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
}