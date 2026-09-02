using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates.Users;

public class KycVerification : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public NinNumber Nin { get; private set; }
    public KycVerificationStatus Status { get; private set; }
    public VerificationResult? IdentityResult { get; private set; }
    public VerificationResult? FacialResult { get; private set; }

    private KycVerification() { }

    public KycVerification(Guid userId, NinNumber nin) : this()
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Nin = nin;
        Status = KycVerificationStatus.Pending;
    }

    public Result ApplyIdentityResult(VerificationResult r)
    {
        IdentityResult = r;
        return TryComplete();
    }

    public Result ApplyFacialResult(VerificationResult r)
    {
        FacialResult = r;
        return TryComplete();
    }

    private Result TryComplete()
    {
        if (IdentityResult is null || FacialResult is null) return Result.Ok(); // not done yet

        Status = IdentityResult.Outcome && FacialResult.Outcome
            ? KycVerificationStatus.Verified
            : KycVerificationStatus.Rejected;

        if (Status == KycVerificationStatus.Verified)
            Raise(new IdentityVerified(UserId, Id));

        return Result.Ok();
    }
}
