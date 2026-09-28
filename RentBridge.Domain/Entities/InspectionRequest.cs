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
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest created: Id={Id}, TenantUserId={TenantUserId}, PreferredDate={PreferredDate}, Status={Status}");
    }

    public Result Confirm(DateTimeOffset? scheduledDate = null, string? notes = null)
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Confirm called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.Pending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Confirm failed: Status={Status} != Pending");
            return Result.Fail("Only pending inspections can be confirmed");
        }
        ScheduledDate = scheduledDate;
        Notes = notes;
        Status = InspectionStatus.Confirmed;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Confirm succeeded. Status now={Status}");
        return Result.Ok();
    }

    public Result Decline()
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Decline called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.Pending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Decline failed: Status={Status} != Pending");
            return Result.Fail("Only pending inspections can be declined");
        }
        Status = InspectionStatus.Declined;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Decline succeeded. Status now={Status}");
        return Result.Ok();
    }

    public Result Cancel()
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Cancel called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.Pending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Cancel failed: Status={Status} != Pending");
            return Result.Fail("Only pending inspections can be cancelled");
        }
        Status = InspectionStatus.Cancelled;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.Cancel succeeded. Status now={Status}");
        return Result.Ok();
    }

    public Result UpdateDetails(DateTimeOffset preferredDate, string? note = null)
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.UpdateDetails called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.Pending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.UpdateDetails failed: Status={Status} != Pending");
            return Result.Fail("Only pending inspections can be updated");
        }
        PreferredDate = preferredDate;
        if (!string.IsNullOrWhiteSpace(note))
            Note = note;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.UpdateDetails succeeded");
        return Result.Ok();
    }

    public Result ProposeReschedule(DateTimeOffset newDate, string? note = null)
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.ProposeReschedule called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.Confirmed)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.ProposeReschedule failed: Status={Status} != Confirmed");
            return Result.Fail("Only a confirmed inspection can be rescheduled");
        }
        ProposedDate = newDate;
        RescheduleNote = note;
        Status = InspectionStatus.ReschedulePending;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.ProposeReschedule succeeded. Status now={Status}");
        return Result.Ok();
    }

    public Result AcceptReschedule()
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.AcceptReschedule called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.ReschedulePending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.AcceptReschedule failed: Status={Status} != ReschedulePending");
            return Result.Fail("Only a reschedule request can be accepted");
        }
        ScheduledDate = ProposedDate;
        Notes = RescheduleNote ?? Notes;
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.AcceptReschedule succeeded. Status now={Status}");
        return Result.Ok();
    }

    public Result RejectReschedule()
    {
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.RejectReschedule called. Id={Id}, CurrentStatus={Status}");
        if (Status != InspectionStatus.ReschedulePending)
        {
            Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.RejectReschedule failed: Status={Status} != ReschedulePending");
            return Result.Fail("Only a reschedule request can be rejected");
        }
        ProposedDate = null;
        RescheduleNote = null;
        Status = InspectionStatus.Confirmed;
        Console.WriteLine($"[DEBUG DOMAIN] InspectionRequest.RejectReschedule succeeded. Status now={Status}");
        return Result.Ok();
    }
}
