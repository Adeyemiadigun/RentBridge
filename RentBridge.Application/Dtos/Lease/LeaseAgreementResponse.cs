using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Lease;

public sealed record LeaseAgreementResponse(
    int Version,
    string ContentHash,
    DateTimeOffset DraftedAt,
    AgreementTerms Terms,
    bool IsCertified,
    bool IsFullySigned,
    Guid? CertifyingLawyerId,
    DateTimeOffset? CertifiedAt,
    IReadOnlyList<SignatureItem> Signatures);