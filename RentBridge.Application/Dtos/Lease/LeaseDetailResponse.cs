using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Lease;

public sealed record LeaseDetailResponse(
    Guid LeaseId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    Guid? AssignedLawyerId,
    LeaseStatus Status,
    DateTimeOffset CreatedAt,
    AgreementDetail Agreement,
    AgreementDocumentItem? AgreementDocument,
    IReadOnlyList<EscrowPaymentItem> EscrowPayments);

public sealed record AgreementDetail(
    bool IsCertified,
    Guid? CertifyingLawyerId,
    DateTimeOffset? CertifiedAt,
    bool IsFullySigned,
    IReadOnlyList<SignatureItem> Signatures);

public sealed record AgreementDocumentItem(int Version, string ContentHash, DateTimeOffset DraftedAt);

public sealed record SignatureItem(LeaseParty Party, DateTimeOffset SignedAt);

public sealed record EscrowPaymentItem(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Currency,
    EscrowStatus Status,
    DateTimeOffset CreatedAt);