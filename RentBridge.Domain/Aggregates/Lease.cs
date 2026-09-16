using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

public class Lease : Entity<Guid>
{
    public Guid ListingId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public Guid LandlordUserId { get; private set; }
    public Guid? AssignedLawyerId { get; private set; }
    public LeaseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // gate receipts
    public DateTimeOffset? IdentityGatePassed { get; private set; }
    public DateTimeOffset? InspectionGatePassed { get; private set; }
    public DateTimeOffset? LegalGatePassed { get; private set; }

    public Agreement Agreement { get; private set; }   // owned entity

    private readonly List<EscrowPayment> _escrowPayments = new();
    public IReadOnlyCollection<EscrowPayment> EscrowPayments => _escrowPayments;

    private readonly List<InspectionRequest> _inspectionRequests = new();
    public IReadOnlyCollection<InspectionRequest> InspectionRequests => _inspectionRequests;

    private Lease() { }

    public Lease(Guid listingId, Guid tenantUserId, Guid landlordUserId) : this()
    {
        Id = Guid.NewGuid();
        ListingId = listingId;
        TenantUserId = tenantUserId;
        LandlordUserId = landlordUserId;
        Status = LeaseStatus.Initiated;
        CreatedAt = DateTimeOffset.UtcNow;
        Agreement = new Agreement(Id);
        Raise(new LeaseCreated(Id));
    }

    public Result RequestInspection(Guid tenantUserId, DateTimeOffset preferredDate, string? note = null)
    {
        if (Status is not (LeaseStatus.Initiated or LeaseStatus.InspectionRequested))
            return Result.Fail("Inspection can only be requested before the inspection is confirmed.");

        if (_inspectionRequests.Any(r => r.Status == InspectionStatus.Pending))
            return Result.Fail("A pending inspection request already exists for this lease.");

        _inspectionRequests.Add(new InspectionRequest(tenantUserId, preferredDate, note));
        Raise(new InspectionRequested(Id, preferredDate));
        return Result.Ok();
    }

    public Result BeginInspectionFlow()
    {
        if (Status != LeaseStatus.Initiated) return Result.Fail("Cannot start inspection from current state.");
        Status = LeaseStatus.InspectionRequested;
        return Result.Ok();
    }

    public Result ConfirmInspection(DateTimeOffset? scheduledDate = null, string? notes = null)
    {
        if (Status != LeaseStatus.InspectionRequested) return Result.Fail("No pending inspection to confirm.");

        var pending = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.Pending);
        if (pending is null) return Result.Fail("No pending inspection request to confirm.");

        var result = pending.Confirm(scheduledDate, notes);
        if (!result.IsSuccess) return result;

        Status = LeaseStatus.InspectionConfirmed;
        InspectionGatePassed = DateTimeOffset.UtcNow;
        Raise(new InspectionConfirmed(Id));
        return Result.Ok();
    }

    public Result DeclinePendingInspection()
    {
        if (Status != LeaseStatus.InspectionRequested) return Result.Fail("No pending inspection flow to decline.");

        var pending = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.Pending);
        if (pending is null) return Result.Fail("No pending inspection request to decline.");

        var result = pending.Decline();
        if (!result.IsSuccess) return result;

        Status = LeaseStatus.Initiated;
        Raise(new InspectionDeclined(Id));
        return Result.Ok();
    }

    public Result CancelPendingInspection(Guid tenantUserId)
    {
        if (tenantUserId != TenantUserId)
            return Result.Fail("Only the tenant on this lease can cancel their inspection request.");

        if (Status != LeaseStatus.InspectionRequested) return Result.Fail("No pending inspection flow to cancel.");

        var pending = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.Pending);
        if (pending is null) return Result.Fail("No pending inspection request to cancel.");

        var result = pending.Cancel();
        if (!result.IsSuccess) return result;

        Status = LeaseStatus.Initiated;
        Raise(new InspectionCancelled(Id));
        return Result.Ok();
    }

    public Result RequestReschedule(Guid tenantUserId, DateTimeOffset newDate, string? note = null)
    {
        if (tenantUserId != TenantUserId)
            return Result.Fail("Only the tenant on this lease can request a reschedule.");

        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("Inspection must be confirmed before rescheduling.");

        var confirmed = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.Confirmed);
        if (confirmed is null) return Result.Fail("No confirmed inspection to reschedule.");

        var result = confirmed.ProposeReschedule(newDate, note);
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduleRequested(Id, newDate));
        return Result.Ok();
    }

    public Result ConfirmReschedule()
    {
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("No confirmed inspection to reschedule.");

        var pending = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.ReschedulePending);
        if (pending is null) return Result.Fail("No pending reschedule request.");

        var result = pending.AcceptReschedule();
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduled(Id, pending.ScheduledDate ?? pending.PreferredDate));
        return Result.Ok();
    }

    public Result RejectReschedule()
    {
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("No confirmed inspection to reschedule.");

        var pending = _inspectionRequests.SingleOrDefault(r => r.Status == InspectionStatus.ReschedulePending);
        if (pending is null) return Result.Fail("No pending reschedule request.");

        var result = pending.RejectReschedule();
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduleRejected(Id));
        return Result.Ok();
    }

    public Result AssignLawyer(Guid lawyerId)
    {
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("Assign a lawyer only after inspection is confirmed.");
        AssignedLawyerId = lawyerId;
        Status = LeaseStatus.LegalReview;
        Raise(new LawyerAssigned(Id, lawyerId));
        return Result.Ok();
    }

    public Result BeginLegalReview()
    {
        if (Status == LeaseStatus.LegalReview) return Result.Ok();
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("Inspection must be confirmed before legal review.");
        Status = LeaseStatus.LegalReview;
        return Result.Ok();
    }

    public Result RecordIdentityGate()
    {
        if (IdentityGatePassed is not null) return Result.Fail("Identity gate already recorded.");
        IdentityGatePassed = DateTimeOffset.UtcNow;
        return Result.Ok();
    }

    public Result Certify(Guid certifyingLawyerId)
    {
        if (Status != LeaseStatus.LegalReview) return Result.Fail("Not in legal review.");
        if (AssignedLawyerId != certifyingLawyerId) return Result.Fail("Wrong lawyer for this lease.");

        var result = Agreement.Certify(certifyingLawyerId);
        if (!result.IsSuccess) return result;

        LegalGatePassed = DateTimeOffset.UtcNow;
        Status = LeaseStatus.Certified;
        Raise(new AgreementCertified(Id));
        return Result.Ok();
    }

    public Result Sign(LeaseParty party, SignatureRecord signature)
    {
        if (Status != LeaseStatus.Certified) return Result.Fail("Agreement must be certified before signing.");

        var result = Agreement.AddSignature(party, signature);
        if (!result.IsSuccess) return result;

        Status = Agreement.IsFullySigned
            ? LeaseStatus.FullySigned
            : LeaseStatus.PartiallySigned;

        if (Agreement.IsFullySigned)
            Raise(new AgreementFullySigned(Id));

        return Result.Ok();
    }

    public Result RecordFunding(EscrowPayment payment)
    {
        if (!Agreement.IsFullySigned)
            return Result.Fail("Both parties must sign before funding escrow.");
        if (_escrowPayments.Any(p => p.Id == payment.Id))
            return Result.Fail("Payment already recorded.");

        _escrowPayments.Add(payment);
        Status = LeaseStatus.FundedInEscrow;
        Raise(new EscrowFunded(Id, payment.Id));
        return Result.Ok();
    }

    public Result MarkReleasing()
    {
        if (Status != LeaseStatus.FundedInEscrow) return Result.Fail("Must be funded to release.");
        Status = LeaseStatus.Releasing;
        return Result.Ok();
    }

    public Result Release()
    {
        if (IdentityGatePassed is null) return Result.Fail("Identity gate not passed.");
        if (InspectionGatePassed is null) return Result.Fail("Inspection gate not passed.");
        if (LegalGatePassed is null) return Result.Fail("Legal review gate not passed.");
        if (Status != LeaseStatus.Releasing) return Result.Fail("Escrow must be in releasing state.");

        Status = LeaseStatus.Released;
        Raise(new EscrowReleased(Id));
        return Result.Ok();
    }
}

public record LeaseCreated(Guid LeaseId) : IDomainEvent;
public record InspectionRequested(Guid LeaseId, DateTimeOffset PreferredDate) : IDomainEvent;
public record InspectionRescheduleRequested(Guid LeaseId, DateTimeOffset ProposedDate) : IDomainEvent;
public record InspectionRescheduled(Guid LeaseId, DateTimeOffset NewDate) : IDomainEvent;
public record InspectionRescheduleRejected(Guid LeaseId) : IDomainEvent;
public record InspectionCancelled(Guid LeaseId) : IDomainEvent;
public record InspectionConfirmed(Guid LeaseId) : IDomainEvent;
public record InspectionDeclined(Guid LeaseId) : IDomainEvent;
public record LawyerAssigned(Guid LeaseId, Guid LawyerId) : IDomainEvent;
public record AgreementCertified(Guid LeaseId) : IDomainEvent;
public record AgreementFullySigned(Guid LeaseId) : IDomainEvent;
public record EscrowFunded(Guid LeaseId, Guid PaymentId) : IDomainEvent;
public record EscrowReleased(Guid LeaseId) : IDomainEvent;
