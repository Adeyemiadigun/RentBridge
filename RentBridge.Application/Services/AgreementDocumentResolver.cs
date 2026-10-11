using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Services;

/// <summary>The document a caller may currently view, if any.</summary>
public sealed record AgreementDocumentReference(string Url, string? FileName, bool IsFinal);

/// <summary>
/// Single source of truth for "which agreement document may this caller see".
/// Shared by the download and inline-view paths so the two can never disagree.
/// </summary>
public static class AgreementDocumentResolver
{
    /// <summary>
    /// Resolves the viewable document for the caller:
    /// <list type="bullet">
    /// <item>Once the flow is finalized, everyone with access sees the retained
    /// final (both-signature) agreement.</item>
    /// <item>The assigned lawyer/admin sees the file currently awaiting their
    /// attention — the source document until the landlord returns one, then the
    /// landlord's signed copy, then the tenant's.</item>
    /// <item>The landlord sees the lawyer's source document; the tenant sees the
    /// landlord's signed copy they are expected to countersign.</item>
    /// </list>
    /// </summary>
    public static AgreementDocumentReference? Resolve(
        LeaseAggregate lease,
        bool isLawyerOrAdmin,
        LeaseParty? party)
    {
        var agreement = lease.Agreement;

        if (agreement.IsFinalized && !string.IsNullOrWhiteSpace(agreement.FinalDocumentUrl))
        {
            return new AgreementDocumentReference(
                agreement.FinalDocumentUrl!, agreement.FinalDocumentFileName, true);
        }

        if (isLawyerOrAdmin)
        {
            (string? Url, string? FileName) current = lease.Status switch
            {
                LeaseStatus.LandlordDocInReview =>
                    (agreement.LandlordReview.UploadedUrl, agreement.LandlordReview.UploadedFileName),
                LeaseStatus.TenantDocInReview or LeaseStatus.TenantSigning =>
                    (agreement.TenantReview.UploadedUrl, agreement.TenantReview.UploadedFileName),
                _ => (agreement.OriginalDocumentUrl, agreement.OriginalDocumentFileName),
            };

            // Fall back to whatever the lawyer uploaded most recently so a review
            // page never dead-ends on an absent pointer.
            if (string.IsNullOrWhiteSpace(current.Url))
            {
                current = !string.IsNullOrWhiteSpace(agreement.OriginalDocumentUrl)
                    ? (agreement.OriginalDocumentUrl, agreement.OriginalDocumentFileName)
                    : (agreement.LandlordReview.UploadedUrl, agreement.LandlordReview.UploadedFileName);
            }

            return string.IsNullOrWhiteSpace(current.Url)
                ? null
                : new AgreementDocumentReference(current.Url!, current.FileName, false);
        }

        if (party == LeaseParty.Tenant)
        {
            var url = agreement.LandlordReview.UploadedUrl;
            return string.IsNullOrWhiteSpace(url)
                ? null
                : new AgreementDocumentReference(url, agreement.LandlordReview.UploadedFileName, false);
        }

        var original = agreement.OriginalDocumentUrl;
        return string.IsNullOrWhiteSpace(original)
            ? null
            : new AgreementDocumentReference(original, agreement.OriginalDocumentFileName, false);
    }
}
