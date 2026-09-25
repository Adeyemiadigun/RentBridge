namespace RentBridge.Application.Dtos.Properties;

public sealed record OwnershipDocumentReviewItem(
    Guid DocumentId,
    string FileKey,
    string Status,
    string? VerifiedByName,
    string? VerifiedByRole,
    string? RejectionReason,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ReviewedAt);

public sealed record PropertyReviewItem(
    Guid PropertyId,
    string Street,
    string City,
    string Area,
    string State,
    string? PropertyType,
    int Bedrooms,
    int Bathrooms,
    bool IsVerified,
    string? VerifiedByUserId,
    string? VerifiedByName,
    string? VerifiedByRole,
    Guid? AssignedLawyerId,
    string? AssignedLawyerName,
    Guid OwnerUserId,
    string OwnerName,
    IReadOnlyList<OwnershipDocumentReviewItem> Documents);