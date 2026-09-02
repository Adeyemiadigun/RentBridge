using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates.Users;

public class Property : Entity<Guid>
{
    public Guid OwnerUserId { get; private set; }
    public Address PropertyAddress { get; private set; }
    private readonly List<OwnershipDocument> _documents = new();
    public IReadOnlyCollection<OwnershipDocument> Documents => _documents;

    private Property() { }

    public Property(Guid ownerUserId, Address address) : this()
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        PropertyAddress = address;
    }

    public void AddDocument(string fileKey)
        => _documents.Add(new OwnershipDocument(Id, fileKey));

    public Result VerifyDocument(Guid docId)
    {
        var doc = _documents.First(d => d.Id == docId);
        doc.Verify();
        if (_documents.Any(d => d.Status == OwnershipDocStatus.Verified))
            Raise(new OwnershipVerified(Id));
        return Result.Ok();
    }
}

public record OwnershipVerified(Guid PropertyId) : IDomainEvent;
