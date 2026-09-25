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
}