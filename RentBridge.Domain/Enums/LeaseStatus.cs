namespace RentBridge.Domain.Enums;

// Domain/Enums/LeaseStatus.cs
public enum LeaseStatus
{
    Initiated, InspectionRequested, InspectionConfirmed, LegalReview,
    Certified, AwaitingSignatures, PartiallySigned,FullySigned, FundedInEscrow,
    Releasing, Released, Cancelled,

    // Document-based signing flow (web): the assigned lawyer uploads the
    // agreement, the landlord signs and returns it, the lawyer approves, then
    // the tenant signs and returns it, and the lawyer approves again before
    // payment opens. Appended (not inserted) so existing stored string values
    // keep mapping to their original members.
    LandlordSigning, LandlordDocInReview, TenantSigning, TenantDocInReview
}
