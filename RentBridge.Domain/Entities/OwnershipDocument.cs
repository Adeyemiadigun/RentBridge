using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Owned child entity of the Property aggregate. Not an aggregate root —
/// it has no independent consistency boundary and is only meaningful
/// within its Property. Persisted via the Property's OwnsMany mapping.
/// </summary>
public class OwnershipDocument
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string FileKey { get; private set; }
    public OwnershipDocStatus Status { get; private set; }
    public string? VerifiedByUserId { get; private set; }
    public string? VerifiedByName { get; private set; }
    public string? VerifiedByRole { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public OwnershipDocument(Guid propertyId, string fileKey)
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        FileKey = fileKey;
        Status = OwnershipDocStatus.Uploaded;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    private OwnershipDocument() { }   // EF

    public Result StartReview()
    {
        if (Status != OwnershipDocStatus.Uploaded)
            return Result.Fail("Only uploaded documents can be submitted for review.");
        Status = OwnershipDocStatus.UnderReview;
        return Result.Ok();
    }

    public Result Verify(string verifierUserId, string verifierName, string verifierRole)
    {
        if (Status != OwnershipDocStatus.UnderReview)
            return Result.Fail("Only documents under review can be verified.");
        Status = OwnershipDocStatus.Verified;
        VerifiedByUserId = verifierUserId;
        VerifiedByName = verifierName;
        VerifiedByRole = verifierRole;
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Ok();
    }

    public Result Reject(string? reason)
    {
        if (Status != OwnershipDocStatus.UnderReview)
            return Result.Fail("Only documents under review can be rejected.");
        Status = OwnershipDocStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Ok();
    }
}
