using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

public class KycVerification : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public NinNumber Nin { get; private set; }
    public KycVerificationStatus Status { get; private set; }
    public VerificationResult? Outcome { get; private set; }

    private KycVerification() { }

    public KycVerification(Guid userId, NinNumber nin) : this()
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Nin = nin;
        Status = KycVerificationStatus.Pending;
    }

    /// <summary>
    /// Applies the verification vendor result. Sync vendors (Dojah) and
    /// webhook vendors (Smile ID) both return one complete outcome per
    /// check (ID check + selfie), so a single result decides.
    /// </summary>
    public Result ApplyResult(VerificationResult result)
    {
        if (Status == KycVerificationStatus.Verified || Status == KycVerificationStatus.Rejected)
        {
            return Result.Fail("This verification has already been completed.");
        }

        Outcome = result;
        Status = result.Outcome
            ? KycVerificationStatus.Verified
            : KycVerificationStatus.Rejected;

        if (Status == KycVerificationStatus.Verified)
        {
            Raise(new IdentityVerified(UserId, Id));
        }

        return Result.Ok();
    }
}