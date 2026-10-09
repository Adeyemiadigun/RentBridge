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
    IReadOnlyList<EscrowPaymentItem> EscrowPayments,
    AssignedLawyerSummary? AssignedLawyer,
    decimal? TotalAmount,
    string? TotalAmountCurrency);

/// <summary>
/// Assigned lawyer contact details. The bar number lives on the lawyer's
/// profile (owned by the User), so it is projected here rather than in the
/// hashed agreement terms — adding it to AgreementTerms.Party would change the
/// pinned content hash of every already-certified agreement.
/// </summary>
public sealed record AssignedLawyerSummary(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? BarNumber);

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
    DateTimeOffset CreatedAt,
    decimal? TotalAmount,
    string? TotalAmountCurrency);