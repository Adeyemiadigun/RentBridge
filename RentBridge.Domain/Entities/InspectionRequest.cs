using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Owned child entity of the Lease aggregate. Not an aggregate root —
/// an inspection request only exists within its Lease. The Lease
/// aggregate coordinates the inspection lifecycle; this entity records
/// a single requested → confirmed → completed inspection. Persisted via the
/// Lease's OwnsMany mapping.
///
/// Two distinct dates, deliberately:
/// <list type="bullet">
///   <item><see cref="ScheduledDate"/> — when the inspection is planned. Required.</item>
///   <item><see cref="ActualDate"/> — when the inspection actually happened. Set by
///   <see cref="Complete"/> only, and this is what stamps the escrow release gate.</item>
/// </list>
/// </summary>
public class InspectionRequest
{
    public Guid Id { get; private set; }
    public Guid TenantUserId { get; private set; }
    public DateTimeOffset PreferredDate { get; private set; }

    /// <summary>When the inspection is planned. Required once confirmed.</summary>
    public DateTimeOffset? ScheduledDate { get; private set; }

    /// <summary>Landlord's notes recorded at confirmation time.</summary>
    public string? Notes { get; private set; }

    public DateTimeOffset? ProposedDate { get; private set; }
    public string? RescheduleNote { get; private set; }

    /// <summary>When the inspection actually took place. Null until completed.</summary>
    public DateTimeOffset? ActualDate { get; private set; }

    /// <summary>Landlord's notes recorded at completion time. Kept separate from <see cref="Notes"/>.</summary>
    public string? CompletionNotes { get; private set; }

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

    public Result Confirm(DateTimeOffset scheduledDate, string? notes = null)
    {
        if (Status != InspectionStatus.Pending)
        {
            return Result.Fail("Only pending inspections can be confirmed");
        }
        ScheduledDate = scheduledDate;
        Notes = notes;
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }

    /// <summary>
    /// Records that the inspection physically took place. Terminal for this
    /// request: once completed it can no longer be rescheduled, declined or
    /// cancelled. This is what satisfies the escrow release gate.
    /// </summary>
    public Result Complete(DateTimeOffset actualDate, string? notes = null)
    {
        if (Status != InspectionStatus.Confirmed)
        {
            return Result.Fail("Only a confirmed inspection can be completed");
        }

        ActualDate = actualDate;
        CompletionNotes = notes;
        Status = InspectionStatus.Completed;
        return Result.Ok();
    }

    public Result Decline()
    {
        if (Status != InspectionStatus.Pending)
        {
            return Result.Fail("Only pending inspections can be declined");
        }
        Status = InspectionStatus.Declined;
        return Result.Ok();
    }

    public Result Cancel()
    {
        if (Status != InspectionStatus.Pending)
        {
            return Result.Fail("Only pending inspections can be cancelled");
        }
        Status = InspectionStatus.Cancelled;
        return Result.Ok();
    }

    public Result UpdateDetails(DateTimeOffset preferredDate, string? note = null)
    {
        if (Status != InspectionStatus.Pending)
        {
            return Result.Fail("Only pending inspections can be updated");
        }
        PreferredDate = preferredDate;
        if (!string.IsNullOrWhiteSpace(note))
            Note = note;
        return Result.Ok();
    }

    public Result ProposeReschedule(DateTimeOffset newDate, string? note = null)
    {
        if (Status != InspectionStatus.Confirmed)
        {
            return Result.Fail("Only a confirmed inspection can be rescheduled");
        }
        ProposedDate = newDate;
        RescheduleNote = note;
        Status = InspectionStatus.ReschedulePending;
        return Result.Ok();
    }

    public Result AcceptReschedule()
    {
        if (Status != InspectionStatus.ReschedulePending)
        {
            return Result.Fail("Only a reschedule request can be accepted");
        }
        // ProposedDate is always set when the status is ReschedulePending, but
        // fall back rather than null out a required date if that ever changes.
        ScheduledDate = ProposedDate ?? ScheduledDate;
        Notes = RescheduleNote ?? Notes;
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }

    public Result RejectReschedule()
    {
        if (Status != InspectionStatus.ReschedulePending)
        {
            return Result.Fail("Only a reschedule request can be rejected");
        }
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        return Result.Ok();
    }
}
