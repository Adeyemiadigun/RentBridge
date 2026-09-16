namespace RentBridge.Domain.Enums;

// Domain/Enums/LeaseStatus.cs
public enum LeaseStatus
{
    Initiated, InspectionRequested, InspectionConfirmed, LegalReview,
    Certified, AwaitingSignatures, PartiallySigned,FullySigned, FundedInEscrow,
    Releasing, Released, Cancelled
}
