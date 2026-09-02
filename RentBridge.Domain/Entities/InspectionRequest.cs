using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Aggregates.Users;

/// <summary>
/// Owned child entity of the Lease aggregate. Not an aggregate root —
/// an inspection request only exists within its Lease. The Lease
/// aggregate coordinates the inspection lifecycle; this entity records
/// a single requested/confirmed/declined inspection. Persisted via the
/// Lease's OwnsMany mapping.
/// </summary>
public class InspectionRequest
{
    public Guid Id { get; private set; }
    public Guid TenantUserId { get; private set; }
    public DateTimeOffset PreferredDate { get; private set; }
    public InspectionStatus Status { get; private set; }
    public string? Note { get; private set; }

    private InspectionRequest() { }   // EF

    public InspectionRequest(Guid tenantUserId, DateTimeOffset preferredDate, string? note = null)
    {
        Id = Guid.NewGuid();
        TenantUserId = tenantUserId;
        PreferredDate = preferredDate;
        Note = note;
        Status = InspectionStatus.Pending;
    }

    public Result Confirm()
    {
        if (Status != InspectionStatus.Pending)
            return Result.Fail("Only pending inspections can be confirmed");
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }

    public Result Decline()
    {
        if (Status != InspectionStatus.Pending)
            return Result.Fail("Only pending inspections can be declined");
        Status = InspectionStatus.Declined;
        return Result.Ok();
    }
}
