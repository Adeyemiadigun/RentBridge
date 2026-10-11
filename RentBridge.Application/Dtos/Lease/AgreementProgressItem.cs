using RentBridge.Domain.Enums;
using AgreementAggregate = RentBridge.Domain.Aggregates.Agreement;
using AgreementReview = RentBridge.Domain.ValueObjects.AgreementPartyReview;

namespace RentBridge.Application.Dtos.Lease;

/// <summary>
/// Document-based signing progress for a lease. Surfaced on the lease list, the
/// lease detail and the agreement response so every dashboard can render the
/// exact step each party is on without fetching the lease individually.
/// </summary>
public sealed record AgreementProgressItem(
    bool HasOriginalDocument,
    string? OriginalDocumentUrl,
    string? OriginalDocumentFileName,
    DateTimeOffset? OriginalDocumentUploadedAt,
    bool IsFinalized,
    string? FinalDocumentUrl,
    string? FinalDocumentFileName,
    DateTimeOffset? FinalizedAt,
    AgreementReviewItem LandlordReview,
    AgreementReviewItem TenantReview)
{
    public static AgreementProgressItem FromAgreement(AgreementAggregate agreement)
        => new(
            !string.IsNullOrWhiteSpace(agreement.OriginalDocumentUrl),
            agreement.OriginalDocumentUrl,
            agreement.OriginalDocumentFileName,
            agreement.OriginalDocumentUploadedAt,
            agreement.IsFinalized,
            agreement.FinalDocumentUrl,
            agreement.FinalDocumentFileName,
            agreement.FinalizedAt,
            AgreementReviewItem.From(agreement.LandlordReview),
            AgreementReviewItem.From(agreement.TenantReview));
}

public sealed record AgreementReviewItem(
    LeaseParty Party,
    DateTimeOffset? DownloadedAt,
    string? UploadedUrl,
    string? UploadedFileName,
    DateTimeOffset? UploadedAt,
    DateTimeOffset? SubmittedForReviewAt,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedByLawyerId)
{
    public static AgreementReviewItem From(AgreementReview review)
        => new(
            review.Party,
            review.DownloadedAt,
            review.UploadedUrl,
            review.UploadedFileName,
            review.UploadedAt,
            review.SubmittedForReviewAt,
            review.ApprovedAt,
            review.ApprovedByLawyerId);
}
