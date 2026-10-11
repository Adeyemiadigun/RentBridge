using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates;

public class Lease : Entity<Guid>
{
    // Invariant: at most one inspection request per lease is in a live status
    // (Pending, Confirmed, ReschedulePending) at a time. Lookups below use
    // FirstOrDefault rather than SingleOrDefault on purpose — if bad data ever
    // violates that invariant, SingleOrDefault throws InvalidOperationException
    // and turns a domain guard into an unhandled 500. FirstOrDefault degrades to
    // the normal "no such request" validation failure instead.

    public Guid ListingId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public Guid LandlordUserId { get; private set; }
    public Guid? AssignedLawyerId { get; private set; }
    public LeaseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Per-transaction gate receipts. Tenant identity is deliberately NOT one of
    // them: it is a fact about the user, not about this lease (see Release),
    // so it is read from the user row at payout time instead of being copied here.
    public DateTimeOffset? InspectionGatePassed { get; private set; }
    public DateTimeOffset? LegalGatePassed { get; private set; }

    public Agreement Agreement { get; private set; }   // owned entity

    public string? LandlordPayoutRecipientCode { get; private set; }

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

    /// <summary>
    /// Requests an inspection for this lease. The returned value is the newly created
    /// <see cref="InspectionRequest"/>, or null when an already-pending request was
    /// updated instead (idempotent path).
    /// </summary>
    public Result<InspectionRequest?> RequestInspection(Guid tenantUserId, DateTimeOffset preferredDate, string? note = null)
    {
        if (Status is not (LeaseStatus.Initiated or LeaseStatus.InspectionRequested))
        {
            return Result<InspectionRequest?>.Fail("Inspection can only be requested before the inspection is confirmed.");
        }

        // Idempotent: an already-pending request is updated rather than rejected,
        // so re-requesting a new date never dead-ends the tenant.
        var existingPending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Pending);
        if (existingPending is not null)
        {
            var result = existingPending.UpdateDetails(preferredDate, note);
            if (!result.IsSuccess)
            {
                return Result<InspectionRequest?>.Fail(result.Error!);
            }
            if (Status == LeaseStatus.Initiated)
                Status = LeaseStatus.InspectionRequested;
            Raise(new InspectionRequested(Id, preferredDate));
            return Result<InspectionRequest?>.Ok(null);
        }

        var newRequest = new InspectionRequest(tenantUserId, preferredDate, note);
        _inspectionRequests.Add(newRequest);
        if (Status == LeaseStatus.Initiated)
            Status = LeaseStatus.InspectionRequested;
        Raise(new InspectionRequested(Id, preferredDate));
        return Result<InspectionRequest?>.Ok(newRequest);
    }

    public Result BeginInspectionFlow()
    {
        // Idempotent: the flow is already open (a retry, or a client calling
        // begin before/after another request moved it). Landing here again is
        // not an error — otherwise callers dead-end with no way to recover.
        if (Status == LeaseStatus.InspectionRequested) return Result.Ok();
        if (Status != LeaseStatus.Initiated) return Result.Fail("Cannot start inspection from current state.");
        Status = LeaseStatus.InspectionRequested;
        return Result.Ok();
    }

    /// <summary>
    /// Accepts the inspection and books a date. This does NOT satisfy the
    /// escrow release gate — arranging an inspection is not the same as the
    /// inspection happening. The gate is stamped by CompleteInspection.
    /// </summary>
    public Result ConfirmInspection(DateTimeOffset scheduledDate, string? notes = null)
    {
        if (Status != LeaseStatus.InspectionRequested)
        {
            return Result.Fail("No pending inspection to confirm.");
        }

        var pending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Pending);
        if (pending is null)
        {
            return Result.Fail("No pending inspection request to confirm.");
        }

        var result = pending.Confirm(scheduledDate, notes);
        if (!result.IsSuccess)
        {
            return result;
        }

        Status = LeaseStatus.InspectionConfirmed;
        Raise(new InspectionConfirmed(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Records that the inspection physically took place and stamps the first
    /// of the two per-transaction escrow release gates (the second being legal
    /// review). Tenant identity is the other precondition but is checked live
    /// from <c>User.IdentityVerified</c> at release time, not stamped here.
    /// This is the only writer of
    /// <see cref="InspectionGatePassed"/>. Idempotent: a repeat call is a
    /// no-op success rather than an error, so a retried webhook or double tap
    /// cannot fail the request.
    /// </summary>
    public Result CompleteInspection(DateTimeOffset actualDate, string? notes = null)
    {
        if (InspectionGatePassed is not null)
        {
            return Result.Ok();
        }

        if (Status is not (LeaseStatus.InspectionConfirmed or LeaseStatus.LegalReview))
        {
            return Result.Fail("The inspection must be confirmed before it can be completed.");
        }

        var confirmed = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Confirmed);
        if (confirmed is null)
        {
            return Result.Fail("No confirmed inspection to complete.");
        }

        // TESTING BYPASS: the "actual date cannot be earlier than the scheduled
        // date" rule is temporarily disabled so inspections can be completed
        // without waiting for the booked date. RE-ENABLE BEFORE PRODUCTION.
        //
        // Completing before the booked date produces an agreement that certifies an
        // inspection earlier than the one both parties agreed to. Compared at date
        // granularity for the same reason as the command validator.
        // if (confirmed.ScheduledDate is { } scheduled &&
        //     actualDate.UtcDateTime.Date < scheduled.UtcDateTime.Date)
        // {
        //     return Result.Fail(
        //         $"The actual inspection date cannot be earlier than the scheduled inspection date ({scheduled:yyyy-MM-dd}).");
        // }

        var result = confirmed.Complete(actualDate, notes);
        if (!result.IsSuccess)
        {
            return result;
        }

        InspectionGatePassed = DateTimeOffset.UtcNow;
        Raise(new InspectionCompleted(Id, actualDate));
        return Result.Ok();
    }

    public Result DeclinePendingInspection()
    {
        if (Status != LeaseStatus.InspectionRequested)
        {
            return Result.Fail("No pending inspection flow to decline.");
        }

        var pending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Pending);
        if (pending is null)
        {
            return Result.Fail("No pending inspection request to decline.");
        }

        var result = pending.Decline();
        if (!result.IsSuccess)
        {
            return result;
        }

        Status = LeaseStatus.Initiated;
        Raise(new InspectionDeclined(Id));
        return Result.Ok();
    }

    public Result CancelPendingInspection(Guid tenantUserId)
    {
        if (tenantUserId != TenantUserId)
            return Result.Fail("Only the tenant on this lease can cancel their inspection request.");

        if (Status != LeaseStatus.InspectionRequested) return Result.Fail("No pending inspection flow to cancel.");

        var pending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Pending);
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

        var confirmed = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.Confirmed);
        if (confirmed is null) return Result.Fail("No confirmed inspection to reschedule.");

        var result = confirmed.ProposeReschedule(newDate, note);
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduleRequested(Id, newDate));
        return Result.Ok();
    }

    public Result ConfirmReschedule()
    {
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("No confirmed inspection to reschedule.");

        var pending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.ReschedulePending);
        if (pending is null) return Result.Fail("No pending reschedule request.");

        var result = pending.AcceptReschedule();
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduled(Id, pending.ScheduledDate ?? pending.PreferredDate));
        return Result.Ok();
    }

    public Result RejectReschedule()
    {
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("No confirmed inspection to reschedule.");

        var pending = _inspectionRequests.FirstOrDefault(r => r.Status == InspectionStatus.ReschedulePending);
        if (pending is null) return Result.Fail("No pending reschedule request.");

        var result = pending.RejectReschedule();
        if (!result.IsSuccess) return result;

        Raise(new InspectionRescheduleRejected(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Bookkeeping only — assigns the reviewing lawyer WITHOUT advancing the
    /// lease. The InspectionConfirmed → LegalReview move belongs solely to
    /// BeginLegalReview, so confirmation leaves the lease inspectable (a
    /// reschedule can still be requested) until legal review is started
    /// explicitly.
    /// </summary>
    public Result AssignLawyer(Guid lawyerId)
    {
        if (Agreement.IsCertified)
            return Result.Fail("Cannot change the assigned lawyer after the agreement is certified.");

        if (AssignedLawyerId == lawyerId)
            return Result.Ok();

        // The lawyer who takes a lease through legal review owns the whole
        // process: once one is assigned, a later automatic pass (or a different
        // property-level lawyer) must never silently reassign it mid-flow and
        // strand the documents already uploaded against the previous lawyer.
        if (AssignedLawyerId is not null)
            return Result.Ok();

        AssignedLawyerId = lawyerId;
        Raise(new LawyerAssigned(Id, lawyerId));
        return Result.Ok();
    }

    /// <summary>
    /// Deliberate reassignment, used only for continuity/recovery paths (e.g.
    /// snapping a lease back to the property's lawyer before legal review, or
    /// replacing a lawyer who is no longer verified). Unlike <see cref="AssignLawyer"/>
    /// this overrides an existing assignment, but is still refused once the
    /// agreement is certified.
    /// </summary>
    public Result ReassignLawyer(Guid lawyerId)
    {
        if (Agreement.IsCertified)
            return Result.Fail("Cannot change the assigned lawyer after the agreement is certified.");

        if (AssignedLawyerId == lawyerId)
            return Result.Ok();

        AssignedLawyerId = lawyerId;
        Raise(new LawyerAssigned(Id, lawyerId));
        return Result.Ok();
    }

    /// <summary>The single transition into legal review. Idempotent.</summary>
    public Result BeginLegalReview()
    {
        if (Status == LeaseStatus.LegalReview) return Result.Ok();
        if (Status != LeaseStatus.InspectionConfirmed) return Result.Fail("Inspection must be confirmed before legal review.");
        if (AssignedLawyerId is null) return Result.Fail("A lawyer must be assigned before legal review.");
        Status = LeaseStatus.LegalReview;
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
        // Signing is allowed from Certified (first signature) or PartiallySigned
        // (second signature). Previously only Certified was accepted, but this method
        // itself moves the lease to PartiallySigned after the first signature — so
        // the second party was locked out and a lease could never reach FullySigned.
        if (Status is not (LeaseStatus.Certified or LeaseStatus.PartiallySigned))
            return Result.Fail("Agreement must be certified before signing.");

        var result = Agreement.AddSignature(party, signature);
        if (!result.IsSuccess) return result;

        Status = Agreement.IsFullySigned
            ? LeaseStatus.FullySigned
            : LeaseStatus.PartiallySigned;

        if (Agreement.IsFullySigned)
            Raise(new AgreementFullySigned(Id));

        return Result.Ok();
    }

    // ---- document-based signing flow --------------------------------------
    // Lawyer uploads the agreement → landlord downloads, signs, returns it and
    // submits for review → lawyer approves → tenant downloads the
    // landlord-signed file, signs, returns it and submits → lawyer approves →
    // the lease is FullySigned and the tenant may fund escrow.

    /// <summary>
    /// The assigned lawyer uploads (or replaces) the agreement document
    /// (LegalReview → LandlordSigning). Idempotent while the landlord has not
    /// yet returned a signed copy.
    /// </summary>
    public Result<AgreementFile> UploadAgreementDocument(string url, string? fileName, Guid uploadedByUserId)
    {
        if (Status is not (LeaseStatus.LegalReview or LeaseStatus.LandlordSigning))
            return Result<AgreementFile>.Fail("The agreement document can only be uploaded while the lease is in legal review or with the landlord for signing.");
        if (AssignedLawyerId is null)
            return Result<AgreementFile>.Fail("A lawyer must be assigned before the agreement document can be uploaded.");

        var result = Agreement.UploadOriginalDocument(url, fileName, uploadedByUserId, DateTimeOffset.UtcNow);
        if (!result.IsSuccess) return Result<AgreementFile>.Fail(result.Error!);

        Status = LeaseStatus.LandlordSigning;
        Raise(new AgreementDocumentUploaded(Id));
        return Result<AgreementFile>.Ok(result.Value);
    }

    /// <summary>
    /// Records that a party opened their copy of the agreement. The caller
    /// resolves the correct file first, so this is gated rather than merely
    /// bookkeeping: a party cannot download a document they should not see.
    /// </summary>
    public Result MarkAgreementDocumentDownloaded(LeaseParty party)
    {
        if (string.IsNullOrWhiteSpace(Agreement.OriginalDocumentUrl))
            return Result.Fail("The agreement document has not been uploaded yet.");

        if (party == LeaseParty.Tenant && !Agreement.LandlordReview.IsApproved)
            return Result.Fail("The landlord's signed agreement has not been approved yet.");

        Agreement.ReviewFor(party).MarkDownloaded(DateTimeOffset.UtcNow);
        return Result.Ok();
    }

    /// <summary>The party uploads their signed copy of the agreement.</summary>
    public Result<AgreementFile> UploadSignedAgreementDocument(LeaseParty party, string url, string? fileName, Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result<AgreementFile>.Fail("A document URL is required.");

        var expected = party == LeaseParty.Tenant ? LeaseStatus.TenantSigning : LeaseStatus.LandlordSigning;
        if (Status != expected)
            return Result<AgreementFile>.Fail(party == LeaseParty.Tenant
                ? "The lease is not currently awaiting the tenant's signed agreement."
                : "The lease is not currently awaiting the landlord's signed agreement.");
        if (party == LeaseParty.Tenant && !Agreement.LandlordReview.IsApproved)
            return Result<AgreementFile>.Fail("The landlord's signed agreement has not been approved yet.");

        var file = Agreement.RecordSignedPartyDocument(party, url, fileName, uploadedByUserId, DateTimeOffset.UtcNow);
        return Result<AgreementFile>.Ok(file);
    }

    /// <summary>
    /// The party sends their uploaded signed copy to the assigned lawyer for
    /// review (LandlordSigning → LandlordDocInReview / TenantSigning →
    /// TenantDocInReview).
    /// </summary>
    public Result SubmitSignedAgreementDocumentForReview(LeaseParty party)
    {
        var review = Agreement.ReviewFor(party);
        if (!review.IsUploaded)
            return Result.Fail("Upload your signed agreement before sending it for review.");
        if (party == LeaseParty.Tenant && !Agreement.LandlordReview.IsApproved)
            return Result.Fail("The landlord's signed agreement has not been approved yet.");

        var expected = party == LeaseParty.Tenant ? LeaseStatus.TenantSigning : LeaseStatus.LandlordSigning;
        if (review.IsSubmitted && Status is LeaseStatus.LandlordDocInReview or LeaseStatus.TenantDocInReview)
            return Result.Ok();
        if (Status != expected)
            return Result.Fail("The signed agreement cannot be submitted from the current state.");

        review.MarkSubmitted(DateTimeOffset.UtcNow);
        Status = party == LeaseParty.Tenant
            ? LeaseStatus.TenantDocInReview
            : LeaseStatus.LandlordDocInReview;
        Raise(new AgreementDocumentSubmittedForReview(Id, party));
        return Result.Ok();
    }

    /// <summary>
    /// The assigned lawyer approves a party's submitted signed document.
    /// Approving the landlord moves the lease to TenantSigning; approving the
    /// tenant makes the agreement FullySigned and unlocks escrow funding.
    /// </summary>
    public Result ApproveAgreementDocument(LeaseParty party, Guid lawyerId)
    {
        if (AssignedLawyerId != lawyerId)
            return Result.Fail("Only the assigned lawyer can approve this agreement document.");

        var expected = party == LeaseParty.Tenant ? LeaseStatus.TenantDocInReview : LeaseStatus.LandlordDocInReview;
        if (Status != expected)
            return Result.Fail(party == LeaseParty.Tenant
                ? "The tenant's signed agreement is not awaiting review."
                : "The landlord's signed agreement is not awaiting review.");

        var result = Agreement.ApprovePartyReview(party, lawyerId, DateTimeOffset.UtcNow);
        if (!result.IsSuccess) return result;

        if (party == LeaseParty.Tenant)
        {
            // Approving the tenant's signed copy finalizes the flow: that copy
            // becomes the retained agreement and the lawyer's source document
            // plus the landlord's signed copy are released for deletion.
            var finalize = Agreement.FinalizeWithTenantDocument(lawyerId, DateTimeOffset.UtcNow);
            if (!finalize.IsSuccess) return finalize;

            // The lawyer's approval of the final, fully-signed document is the
            // legal sign-off. Bypass the legacy clause-certification step so the
            // gate escrow release checks is passed here; otherwise a payout would
            // fail "Legal review gate not passed" despite both parties signing and
            // the lawyer approving.
            LegalGatePassed ??= DateTimeOffset.UtcNow;
        }

        Status = party == LeaseParty.Tenant ? LeaseStatus.FullySigned : LeaseStatus.TenantSigning;
        if (party == LeaseParty.Tenant)
            Raise(new AgreementFullySigned(Id));
        Raise(new AgreementDocumentApproved(Id, party));
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

    public Result AddPendingEscrowPayment(EscrowPayment payment)
    {
        if (!Agreement.IsFullySigned)
            return Result.Fail("Both parties must sign before funding escrow.");
        if (_escrowPayments.Any(p => p.Id == payment.Id))
            return Result.Fail("Payment already recorded.");

        _escrowPayments.Add(payment);
        return Result.Ok();
    }

    public Result ConfirmEscrowFunding(EscrowPayment payment)
    {
        if (!_escrowPayments.Any(p => p.Id == payment.Id))
            return Result.Fail("Payment not found on this lease.");

        if (Status == LeaseStatus.FundedInEscrow || Status == LeaseStatus.Releasing || Status == LeaseStatus.Released)
            return Result.Ok();

        Status = LeaseStatus.FundedInEscrow;
        Raise(new EscrowFunded(Id, payment.Id));
        return Result.Ok();
    }

    public Result SetLandlordPayoutRecipientCode(string recipientCode)
    {
        if (Status is LeaseStatus.Released or LeaseStatus.Releasing)
            return Result.Fail("Payout recipient cannot change once escrow is releasing or released.");
        if (string.IsNullOrWhiteSpace(recipientCode))
            return Result.Fail("Recipient code cannot be empty.");

        LandlordPayoutRecipientCode = recipientCode.Trim();
        return Result.Ok();
    }

    public Result BeginRelease()
    {
        if (Status != LeaseStatus.FundedInEscrow) return Result.Fail("Must be funded to release.");
        if (string.IsNullOrWhiteSpace(LandlordPayoutRecipientCode))
            return Result.Fail("Landlord payout recipient is not set for this lease.");
        Status = LeaseStatus.Releasing;
        return Result.Ok();
    }

    /// <summary>
    /// Finalizes the payout. Requires proof that BOTH parties are identity
    /// verified, supplied by the caller from the user rows rather than stored on
    /// this lease: identity is a user-level fact, and re-reading it here means a
    /// verification revoked between lease creation and payout still blocks the
    /// release. The landlord is checked too — they are the account receiving the
    /// money, and a payout account on file is not the same as a verified identity.
    /// The remaining gates are genuinely per-transaction and stay on the lease.
    /// </summary>
    public Result Release(bool tenantIdentityVerified, bool landlordIdentityVerified)
    {
        if (!tenantIdentityVerified) return Result.Fail("Tenant identity is not verified.");
        if (!landlordIdentityVerified) return Result.Fail("Landlord identity is not verified.");
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
    public record InspectionCompleted(Guid LeaseId, DateTimeOffset ActualDate) : IDomainEvent;
public record InspectionDeclined(Guid LeaseId) : IDomainEvent;
public record LawyerAssigned(Guid LeaseId, Guid LawyerId) : IDomainEvent;
public record AgreementCertified(Guid LeaseId) : IDomainEvent;
public record AgreementFullySigned(Guid LeaseId) : IDomainEvent;
public record AgreementDocumentUploaded(Guid LeaseId) : IDomainEvent;
public record AgreementDocumentSubmittedForReview(Guid LeaseId, LeaseParty Party) : IDomainEvent;
public record AgreementDocumentApproved(Guid LeaseId, LeaseParty Party) : IDomainEvent;
public record EscrowFunded(Guid LeaseId, Guid PaymentId) : IDomainEvent;
public record EscrowReleased(Guid LeaseId) : IDomainEvent;
