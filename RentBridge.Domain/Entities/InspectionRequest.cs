using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Entities;

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
    public DateTimeOffset? ScheduledDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? ProposedDate { get; private set; }
    public string? RescheduleNote { get; private set; }
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

    public Result Confirm(DateTimeOffset? scheduledDate = null, string? notes = null)
    {
        if (Status != InspectionStatus.Pending)
            return Result.Fail("Only pending inspections can be confirmed");
        ScheduledDate = scheduledDate;
        Notes = notes;
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

    public Result Cancel()
    {
        if (Status != InspectionStatus.Pending)
            return Result.Fail("Only pending inspections can be cancelled");
        Status = InspectionStatus.Cancelled;
        return Result.Ok();
    }

    public Result ProposeReschedule(DateTimeOffset newDate, string? note = null)
    {
        if (Status != InspectionStatus.Confirmed)
            return Result.Fail("Only a confirmed inspection can be rescheduled");
        ProposedDate = newDate;
        RescheduleNote = note;
        Status = InspectionStatus.ReschedulePending;
        return Result.Ok();
    }

    public Result AcceptReschedule()
    {
        if (Status != InspectionStatus.ReschedulePending)
            return Result.Fail("Only a reschedule request can be accepted");
        ScheduledDate = ProposedDate;
        Notes = RescheduleNote ?? Notes;
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }

    public Result RejectReschedule()
    {
        if (Status != InspectionStatus.ReschedulePending)
            return Result.Fail("Only a reschedule request can be rejected");
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }
}
