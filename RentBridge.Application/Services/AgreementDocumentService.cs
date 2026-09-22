using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Options;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Services;

public sealed class AgreementDocumentService(
    IUnitOfWork unitOfWork,
    HashingOptions hashingOptions) : IAgreementDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly IUnitOfWork unitOfWork = unitOfWork;
    private readonly HashingOptions hashingOptions = hashingOptions;

    public async Task<Result<AgreementDocument>> EnsureComposedAsync(Lease lease, CancellationToken cancellationToken)
    {
        if (lease.Agreement.Document is not null)
        {
            return Result<AgreementDocument>.Ok(lease.Agreement.Document);
        }

        var listing = await unitOfWork.Repository<Listing>()
            .FirstOrDefault(l => l.Id == lease.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result<AgreementDocument>.Fail("Listing for this lease was not found.");
        }

        var property = await unitOfWork.Repository<Property>()
            .FirstOrDefault(p => p.Id == listing.PropertyId, cancellationToken);
        if (property is null)
        {
            return Result<AgreementDocument>.Fail("Property for this lease was not found.");
        }

        var tenant = await unitOfWork.Repository<Domain.Aggregates.User>()
            .FirstOrDefault(u => u.Id == lease.TenantUserId, cancellationToken);
        if (tenant is null)
        {
            return Result<AgreementDocument>.Fail("Tenant for this lease was not found.");
        }

        var landlord = await unitOfWork.Repository<Domain.Aggregates.User>()
            .FirstOrDefault(u => u.Id == lease.LandlordUserId, cancellationToken);
        if (landlord is null)
        {
            return Result<AgreementDocument>.Fail("Landlord for this lease was not found.");
        }

        var lawyer = lease.AssignedLawyerId is null
            ? null
            : await unitOfWork.Repository<Domain.Aggregates.User>()
                .FirstOrDefault(u => u.Id == lease.AssignedLawyerId, cancellationToken);

        var terms = BuildTerms(lease, listing, property, tenant, landlord, lawyer);
        var json = JsonSerializer.Serialize(terms, JsonOptions);

        var secretKey = hashingOptions.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Hashing:SecretKey is not configured.");
        }

        // Keyed hash (HMAC-SHA256): verifiable only by code that holds the key,
        // so a DB-level change to the JSON cannot be silently re-stamped.
        var hash = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secretKey),
            Encoding.UTF8.GetBytes(json)));

        var version = (lease.Agreement.Document?.Version ?? 0) + 1;
        var document = new AgreementDocument(lease.Agreement.Id, version, json, hash);

        var set = lease.Agreement.SetDocument(document);
        if (!set.IsSuccess)
        {
            return Result<AgreementDocument>.Fail(set.Error!);
        }

        return Result<AgreementDocument>.Ok(document);
    }

    private static AgreementTerms BuildTerms(
        Lease lease,
        Listing listing,
        Property property,
        Domain.Aggregates.User tenant,
        Domain.Aggregates.User landlord,
        Domain.Aggregates.User? lawyer)
    {
        return new AgreementTerms(
            lease.Id,
            listing.Id,
            new Party(
                $"{tenant.FirstName} {tenant.LastName}".Trim(),
                tenant.Email.Value,
                tenant.Phone.Value,
                "Tenant"),
            new Party(
                $"{landlord.FirstName} {landlord.LastName}".Trim(),
                landlord.Email.Value,
                landlord.Phone.Value,
                "Landlord"),
            lawyer is null
                ? null
                : new Party(
                    $"{lawyer.FirstName} {lawyer.LastName}".Trim(),
                    lawyer.Email.Value,
                    lawyer.Phone.Value,
                    "Lawyer"),
            new PropertyTerms(
                listing.Title,
                listing.Description,
                BuildAddress(property.PropertyAddress)),
            new RentTerms(listing.Price.Amount, listing.Price.Currency),
            lease.CreatedAt,
            lease.InspectionRequests
                .FirstOrDefault(r => r.Status == InspectionStatus.Confirmed)
                ?.ScheduledDate,
            DateTimeOffset.UtcNow);
    }

    private static string BuildAddress(Address address)
    {
        var parts = new[] { address.Street, address.City, address.Area, address.State }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(", ", parts);
    }
}