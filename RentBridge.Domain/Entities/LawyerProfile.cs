using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Aggregates.Users;

/// <summary>
/// Owned child of the User aggregate (1:1, only present for lawyers).
/// Not an aggregate root — a lawyer profile has no independent consistency
/// boundary; it is persisted as part of its User (OwnsOne mapping).
/// </summary>
public class LawyerProfile
{
    public string BarNumber { get; private set; }
    public LawyerStatus Status { get; private set; }

    private LawyerProfile() { }   // EF

    public LawyerProfile(string barNumber)
    {
        BarNumber = barNumber;
        Status = LawyerStatus.Pending;
    }

    public Result Verify()
    {
        if (Status != LawyerStatus.Pending)
            return Result.Fail("Only pending lawyers can be verified.");
        Status = LawyerStatus.Verified;
        return Result.Ok();
    }

    public Result Suspend()
    {
        if (Status == LawyerStatus.Suspended)
            return Result.Fail("Lawyer is already suspended.");
        Status = LawyerStatus.Suspended;
        return Result.Ok();
    }

    public Result Reject()
    {
        if (Status != LawyerStatus.Pending)
            return Result.Fail("Only pending lawyers can be rejected.");
        Status = LawyerStatus.Rejected;
        return Result.Ok();
    }
}
