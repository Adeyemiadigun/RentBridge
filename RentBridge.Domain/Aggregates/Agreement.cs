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

    private readonly List<SignatureRecord> _signatures = new();
    public IReadOnlyCollection<SignatureRecord> Signatures => _signatures;

    private Agreement() { }

    public Agreement(Guid leaseId)
    {
        Id = Guid.NewGuid();
        LeaseId = leaseId;
    }

    public bool IsCertified => CertifyingLawyerId is not null;
    public bool IsFullySigned => _signatures.Count >= 2;

    public Result Certify(Guid certifyingLawyerId)
    {
        if (IsCertified) return Result.Fail("Agreement is already certified.");
        if (_signatures.Count > 0) return Result.Fail("Cannot certify an agreement after signing has begun.");

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
