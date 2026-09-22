using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

public class Property : Entity<Guid>
{
    public Guid OwnerUserId { get; private set; }
    public Address PropertyAddress { get; private set; }
    private readonly List<OwnershipDocument> _documents = new();
    public IReadOnlyCollection<OwnershipDocument> Documents => _documents;

    public bool IsVerified { get; private set; } = false;

    public Guid? VerificationLawyerId { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    private Property() { }

    public Property(Guid ownerUserId, string street, string city, string area, string state) : this()
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        PropertyAddress = new Address(street, city, area, state);
    }

public void AddDocument(string fileKey)
        => _documents.Add(new OwnershipDocument(Id, fileKey));

    public Result AssignVerificationLawyer(Guid lawyerId)
    {
        if (IsVerified)
            return Result.Fail("Cannot change verification lawyer after the property has been verified.");
        if (VerificationLawyerId == lawyerId)
            return Result.Ok();

        var previous = VerificationLawyerId;
        VerificationLawyerId = lawyerId;
        Raise(new VerificationLawyerAssigned(Id, previous, lawyerId));
        return Result.Ok();
    }

    public Result StartDocumentReview(Guid docId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == docId);
        if (doc is null) return Result.Fail("Document not found");
        return doc.StartReview();
    }

    public static readonly UserRole[] CanCreateProperty =
    { UserRole.Landlord, UserRole.Caretaker, UserRole.Agent };

    public static bool CanCreateBy(UserRole role) => CanCreateProperty.Contains(role);

    public Result VerifyDocument(Guid docId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == docId);

        if (doc is null) return Result.Fail("Document not found");
        if (doc.Status != OwnershipDocStatus.UnderReview)
            return Result.Fail("Document is not under review.");
        if (doc.Status == OwnershipDocStatus.Verified)
            return Result.Fail("Document is already verified.");
        if (doc.Status == OwnershipDocStatus.Rejected)
            return Result.Fail("Document is rejected.");
        return doc.Verify();            // flip just this doc to Verified
    }

    public Result RejectDocument(Guid docId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == docId);

        if (doc is null) return Result.Fail("Document not found");
        return doc.Reject();
    }

    public Result MarkOwnershipVerified()
    {
        if (IsVerified) return Result.Ok();
        if (_documents.Any(d => d.Status == OwnershipDocStatus.Rejected))
            return Result.Fail("Cannot verify property while a document is rejected.");
        if (_documents.Any(d => d.Status is (OwnershipDocStatus.UnderReview or OwnershipDocStatus.Uploaded)))
            return Result.Fail("Cannot verify property while a document is under review.");
        IsVerified = true;
        VerifiedAt = DateTimeOffset.UtcNow;
        Raise(new OwnershipVerified(Id));     // the ONE place this event is raised
        return Result.Ok();
    }
}

public record OwnershipVerified(Guid PropertyId) : IDomainEvent;
public record VerificationLawyerAssigned(Guid PropertyId, Guid? PreviousLawyerId, Guid LawyerId) : IDomainEvent;
