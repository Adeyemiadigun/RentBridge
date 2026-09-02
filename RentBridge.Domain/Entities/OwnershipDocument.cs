using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Aggregates.Users;

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
    public DateTimeOffset UploadedAt { get; private set; }

    public OwnershipDocument(Guid propertyId, string fileKey)
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        FileKey = fileKey;
        Status = OwnershipDocStatus.Uploaded;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    private OwnershipDocument() { }   // EF

    public Result Verify()
    {
        Status = OwnershipDocStatus.Verified;
        return Result.Ok();
    }
}
