namespace RentBridge.Application.Dtos.Lease;

/// <summary>
/// Canonical, serialized content of a tenancy agreement. Property order matters:
/// the identical JSON is SHA-256 hashed and pinned at certification/signing.
/// </summary>
public sealed record AgreementTerms(
    Guid LeaseId,
    Guid ListingId,
    Party Tenant,
    Party Landlord,
    Party? Lawyer,
    PropertyTerms Property,
    RentTerms Rent,
    DateTimeOffset LeaseCreatedAt,
    DateTimeOffset? InspectionScheduledDate,
    DateTimeOffset DraftedAt);

public sealed record Party(string Name, string Email, string? Phone, string Role);

public sealed record PropertyTerms(string Title, string? Description, string Address);

public sealed record RentTerms(decimal Amount, string Currency);