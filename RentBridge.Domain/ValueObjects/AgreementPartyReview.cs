using RentBridge.Domain.Enums;

namespace RentBridge.Domain.ValueObjects;

/// <summary>
/// Per-party state for the document-based signing flow. One instance per party
/// (landlord, tenant) is owned by the Agreement, so the audit trail of exactly
/// when each party downloaded, uploaded, submitted and was approved is kept
/// alongside the agreement rather than being re-derived from the lease status.
///
/// Mutable by design: each field is stamped by a single Lease transition, and
/// the UI reads these stamps to tell a party precisely which step it is on.
/// </summary>
public class AgreementPartyReview
{
    public LeaseParty Party { get; private set; }
    public DateTimeOffset? DownloadedAt { get; private set; }
    public string? UploadedUrl { get; private set; }
    public string? UploadedFileName { get; private set; }
    public DateTimeOffset? UploadedAt { get; private set; }
    public DateTimeOffset? SubmittedForReviewAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedByLawyerId { get; private set; }

    private AgreementPartyReview() { } // EF

    public AgreementPartyReview(LeaseParty party)
    {
        Party = party;
    }

    public bool IsUploaded => UploadedAt is not null && !string.IsNullOrWhiteSpace(UploadedUrl);
    public bool IsSubmitted => SubmittedForReviewAt is not null;
    public bool IsApproved => ApprovedAt is not null;

    public void MarkDownloaded(DateTimeOffset at)
    {
        DownloadedAt ??= at;
    }

    public void RecordUpload(string url, string? fileName, DateTimeOffset at)
    {
        UploadedUrl = url;
        UploadedFileName = fileName;
        UploadedAt = at;
        // A fresh upload supersedes any earlier submission/approval of the same
        // party's document; it must go through review again.
        SubmittedForReviewAt = null;
        ApprovedAt = null;
        ApprovedByLawyerId = null;
    }

    /// <summary>
    /// Clears the upload pointer without touching the audit stamps (downloaded,
    /// submitted, approved). Used when the referenced bytes are superseded and
    /// deleted — e.g. the landlord's signed copy once the final agreement is
    /// retained — so the UI cannot offer a document that no longer exists.
    /// </summary>
    public void ClearUpload()
    {
        UploadedUrl = null;
        UploadedFileName = null;
    }

    public void MarkSubmitted(DateTimeOffset at)
    {
        SubmittedForReviewAt ??= at;
    }

    public void Approve(Guid lawyerId, DateTimeOffset at)
    {
        SubmittedForReviewAt ??= at;
        ApprovedAt = at;
        ApprovedByLawyerId = lawyerId;
    }

    /// <summary>Clears all progress — used when the lawyer replaces the source document.</summary>
    public void Reset()
    {
        DownloadedAt = null;
        UploadedUrl = null;
        UploadedFileName = null;
        UploadedAt = null;
        SubmittedForReviewAt = null;
        ApprovedAt = null;
        ApprovedByLawyerId = null;
    }
}
