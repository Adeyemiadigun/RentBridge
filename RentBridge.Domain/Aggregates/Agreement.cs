using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

/// <summary>
/// Owned by the Lease aggregate. Enforces the business rule that a
/// signed/certified agreement cannot be edited — changes require a new
/// addendum. Not an aggregate root; persisted via the Lease's OwnsOne mapping.
/// </summary>
public class Agreement
{
    public Guid Id { get; private set; }
    public Guid LeaseId { get; private set; }
    public string? ContentHash { get; private set; }
    public Guid? CertifyingLawyerId { get; private set; }
    public DateTimeOffset? CertifiedAt { get; private set; }

    public AgreementDocument? Document { get; private set; }

    // ---- document-based signing flow ---------------------------------------

    /// <summary>
    /// The source document the assigned lawyer uploads for the landlord to
    /// download, sign and return. Kept separate from <see cref="Document"/>
    /// (which is the hashed terms snapshot used by the legacy certify path).
    /// </summary>
    public string? OriginalDocumentUrl { get; private set; }
    public string? OriginalDocumentFileName { get; private set; }
    public DateTimeOffset? OriginalDocumentUploadedAt { get; private set; }

    /// <summary>
    /// The retained final agreement (the tenant-signed copy bearing both
    /// signatures). Set when the flow is finalized; the intermediate files are
    /// purged at the same time.
    /// </summary>
    public string? FinalDocumentUrl { get; private set; }
    public string? FinalDocumentFileName { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    /// <summary>Per-party progress through download → upload → submit → approve.</summary>
    public AgreementPartyReview LandlordReview { get; private set; } = null!;
    public AgreementPartyReview TenantReview { get; private set; } = null!;

    // Every uploaded file, retained as its own row so a superseded upload is
    // never silently overwritten (which used to orphan the old bytes in
    // storage). Intermediate files are flagged for deletion once finalized.
    private readonly List<AgreementFile> _files = new();
    public IReadOnlyCollection<AgreementFile> Files => _files;

    private readonly List<SignatureRecord> _signatures = new();
    public IReadOnlyCollection<SignatureRecord> Signatures => _signatures;

    private Agreement() { }

    public Agreement(Guid leaseId)
    {
        Id = Guid.NewGuid();
        LeaseId = leaseId;
        LandlordReview = new AgreementPartyReview(LeaseParty.Landlord);
        TenantReview = new AgreementPartyReview(LeaseParty.Tenant);
    }

    public bool IsCertified => CertifyingLawyerId is not null;

    public AgreementPartyReview ReviewFor(LeaseParty party)
        => party == LeaseParty.Tenant ? TenantReview : LandlordReview;

    /// <summary>True once the assigned lawyer has approved both parties' signed documents.</summary>
    public bool IsDocumentFlowComplete => LandlordReview.IsApproved && TenantReview.IsApproved;

    /// <summary>
    /// Fully signed means EITHER the document flow has been approved end-to-end
    /// OR (legacy certify-and-sign path) both signatures are present. The two
    /// are kept in OR so the mobile certify/sign client keeps working while the
    /// web document flow gates escrow on lawyer approval of both documents.
    /// </summary>
    public bool IsFullySigned => _signatures.Count >= 2 || IsDocumentFlowComplete;

    public Result<AgreementFile> UploadOriginalDocument(string url, string? fileName, Guid uploadedByUserId, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(url)) return Result<AgreementFile>.Fail("A document URL is required.");

        OriginalDocumentUrl = url;
        OriginalDocumentFileName = fileName;
        OriginalDocumentUploadedAt = at;

        // Retain the upload as an auditable row and schedule any previous source
        // document for deletion rather than overwriting its URL and orphaning it.
        var file = RecordFile(AgreementFileKind.Original, url, fileName, uploadedByUserId, at);

        // Replacing the source document invalidates any progress made against the
        // previous one — each party must start again from the new file. Their
        // earlier signed copies are now obsolete, so their bytes are released too.
        foreach (var previous in _files.Where(f =>
                     f.Kind != AgreementFileKind.Original && !f.IsPendingDeletion))
        {
            previous.MarkPendingDeletion(at);
        }

        LandlordReview.Reset();
        TenantReview.Reset();
        return Result<AgreementFile>.Ok(file);
    }

    /// <summary>
    /// True once the flow has been finalized: the tenant-signed copy has been
    /// approved and is now the single retained agreement.
    /// </summary>
    public bool IsFinalized => FinalizedAt is not null;

    public bool HasPendingFileDeletions => _files.Any(f => f.IsPendingDeletion);

    /// <summary>
    /// Records an uploaded file. Any earlier file of the same kind (e.g. a
    /// re-upload by the same party) is superseded and flagged for deletion so
    /// its bytes do not linger in object storage.
    /// </summary>
    private AgreementFile RecordFile(
        AgreementFileKind kind,
        string url,
        string? fileName,
        Guid uploadedByUserId,
        DateTimeOffset at)
    {
        foreach (var previous in _files.Where(f => f.Kind == kind && !f.IsFinal))
        {
            previous.MarkPendingDeletion(at);
        }

        var file = new AgreementFile(Guid.NewGuid(), kind, url, fileName, uploadedByUserId, at);
        _files.Add(file);
        return file;
    }

    /// <summary>
    /// Finalizes the flow on approval of the tenant's signed copy: that copy is
    /// retained as the final agreement and every intermediate file (the
    /// lawyer's source document and the landlord's signed copy) is scheduled for
    /// deletion. Their dangling URLs are cleared so the UI can never offer a
    /// document whose bytes are about to disappear.
    /// </summary>
    public Result FinalizeWithTenantDocument(Guid lawyerId, DateTimeOffset at)
    {
        if (IsFinalized) return Result.Ok();

        var tenantFile = _files
            .Where(f => f.Kind == AgreementFileKind.TenantSigned && !f.IsPendingDeletion)
            .OrderByDescending(f => f.UploadedAt)
            .FirstOrDefault();
        if (tenantFile is null)
            return Result.Fail("The tenant's signed document has not been uploaded yet.");

        tenantFile.MarkFinal();
        FinalDocumentUrl = tenantFile.Url;
        FinalDocumentFileName = tenantFile.FileName;
        FinalizedAt = at;

        foreach (var file in _files.Where(f => f.Kind != AgreementFileKind.TenantSigned))
        {
            file.MarkPendingDeletion(at);
        }

        // Intermediate URLs no longer resolve to a retained file.
        OriginalDocumentUrl = null;
        OriginalDocumentFileName = null;
        OriginalDocumentUploadedAt = null;
        LandlordReview.ClearUpload();

        return Result.Ok();
    }

    /// <summary>
    /// Removes a file row after its bytes have been deleted from object storage.
    /// Verification (was flagged for deletion) lives in the cleanup service; the
    /// aggregate just drops the row.
    /// </summary>
    public void AcknowledgeFileDeleted(Guid fileId)
    {
        var file = _files.FirstOrDefault(f => f.Id == fileId);
        if (file is not null && !file.IsFinal)
        {
            _files.Remove(file);
        }
    }

    /// <summary>
    /// Records a party's signed upload: sets the review pointer (what the party
    /// currently has on file) and retains the upload as an auditable row,
    /// superseding any earlier copy from the same party.
    /// </summary>
    public AgreementFile RecordSignedPartyDocument(
        LeaseParty party,
        string url,
        string? fileName,
        Guid uploadedByUserId,
        DateTimeOffset at)
    {
        var kind = party == LeaseParty.Tenant
            ? AgreementFileKind.TenantSigned
            : AgreementFileKind.LandlordSigned;

        var file = RecordFile(kind, url, fileName, uploadedByUserId, at);
        ReviewFor(party).RecordUpload(url, fileName, at);
        return file;
    }

    public Result ApprovePartyReview(LeaseParty party, Guid lawyerId, DateTimeOffset at)
    {
        var review = ReviewFor(party);
        if (!review.IsSubmitted)
            return Result.Fail("That party's signed document has not been submitted for review yet.");

        if (review.IsApproved) return Result.Ok();

        review.Approve(lawyerId, at);
        return Result.Ok();
    }

    public Result SetDocument(AgreementDocument document)
    {
        if (IsCertified) return Result.Fail("Cannot change agreement content after certification.");
        if (_signatures.Count > 0) return Result.Fail("Cannot change agreement content after signing has begun.");

        Document = document;
        ContentHash = document.ContentHash;
        return Result.Ok();
    }

    public Result Certify(Guid certifyingLawyerId)
    {
        if (IsCertified) return Result.Fail("Agreement is already certified.");
        if (_signatures.Count > 0) return Result.Fail("Cannot certify an agreement after signing has begun.");
        if (Document is null || string.IsNullOrWhiteSpace(Document.ContentHash))
            return Result.Fail("Agreement content has not been composed yet.");

        CertifyingLawyerId = certifyingLawyerId;
        CertifiedAt = DateTimeOffset.UtcNow;
        return Result.Ok();
    }

    public Result AddSignature(LeaseParty party, SignatureRecord signature)
    {
        if (!IsCertified) return Result.Fail("Agreement must be certified before signing.");
        if (_signatures.Any(s => s.Party == party))
            return Result.Fail("This party has already signed.");
        if (signature.Party != party)
            return Result.Fail("Signature party mismatch.");

        _signatures.Add(signature);
        return Result.Ok();
    }

    public bool HasSigned(LeaseParty party) => _signatures.Any(s => s.Party == party);
}
