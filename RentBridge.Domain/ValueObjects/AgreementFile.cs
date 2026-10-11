using RentBridge.Domain.Enums;

namespace RentBridge.Domain.ValueObjects;

/// <summary>
/// A single uploaded file recorded in the document-based signing flow. Owned by
/// the Agreement (its own table), so every upload is retained as an audit row
/// instead of being overwritten in place — which previously lost the URL of a
/// superseded file and orphaned it in object storage forever.
///
/// Lifecycle: a file is created by <c>Agreement.RecordFile</c>, superseded
/// files are flagged <see cref="IsPendingDeletion"/>, and the retained final
/// file is flagged <see cref="IsFinal"/>. The cleanup service deletes the bytes
/// from object storage and then removes the row.
/// </summary>
public class AgreementFile
{
    public Guid Id { get; private set; }
    public AgreementFileKind Kind { get; private set; }
    public string Url { get; private set; } = null!;
    public string? FileName { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }

    /// <summary>The retained copy: the tenant-signed file that ends the flow.</summary>
    public bool IsFinal { get; private set; }

    /// <summary>When set, the bytes should be deleted from storage and the row purged.</summary>
    public DateTimeOffset? PendingDeletionAt { get; private set; }

    private AgreementFile() { } // EF

    public AgreementFile(
        Guid id,
        AgreementFileKind kind,
        string url,
        string? fileName,
        Guid uploadedByUserId,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        Kind = kind;
        Url = url;
        FileName = fileName;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = uploadedAt;
    }

    public bool IsPendingDeletion => PendingDeletionAt is not null;

    public void MarkFinal() => IsFinal = true;

    public void MarkPendingDeletion(DateTimeOffset at) => PendingDeletionAt ??= at;
}
