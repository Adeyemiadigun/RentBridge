using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;
using PropertyAggregate = RentBridge.Domain.Aggregates.Property;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Query.Admin;

public sealed class GetAdminPayoutsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetAdminPayoutsQueryHandler> logger)
    : IRequestHandler<GetAdminPayoutsQuery, Result<PagedResult<AdminPayoutItem>>>
{
    public async Task<Result<PagedResult<AdminPayoutItem>>> Handle(
        GetAdminPayoutsQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<PagedResult<AdminPayoutItem>>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to list payouts without admin role", actor.Id);
            return Result<PagedResult<AdminPayoutItem>>.Forbid("Only an admin can view payouts.");
        }

        var (leases, total) = await unitOfWork.Leases.GetPagedWithEscrowPaymentsAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        // Flatten to the latest payment per lease, then filter by status if asked.
        // The payout console is about payouts, so payments that never reached a
        // payout-relevant state are dropped.
        var rows = leases
            .Select(lease => new
            {
                Lease = lease,
                Payment = lease.EscrowPayments
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()
            })
            .Where(x => x.Payment is not null
                && x.Payment.Status is EscrowStatus.Funded
                    or EscrowStatus.Releasing
                    or EscrowStatus.Released
                    or EscrowStatus.PayoutFailed)
            .Where(x => request.Status is null || x.Payment!.Status == request.Status)
            .ToList();

        // One batch lookup per related table — no per-row queries.
        var listingIds = rows.Select(r => r.Lease.ListingId).Distinct().ToList();
        var listings = await unitOfWork.Repository<ListingAggregate>()
            .FindAsync(l => listingIds.Contains(l.Id), cancellationToken);
        var listingsById = listings.ToDictionary(l => l.Id);

        var propertyIds = listings.Select(l => l.PropertyId).Distinct().ToList();
        var properties = await unitOfWork.Repository<PropertyAggregate>()
            .FindAsync(p => propertyIds.Contains(p.Id), cancellationToken);
        var propertiesById = properties.ToDictionary(p => p.Id);

        var userIds = rows
            .SelectMany(r => new[] { r.Lease.TenantUserId, r.Lease.LandlordUserId })
            .Distinct()
            .ToList();
        var users = await unitOfWork.Repository<UserAggregate>()
            .FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var usersById = users.ToDictionary(u => u.Id, u => u);

        var items = rows.Select(r =>
        {
            var payment = r.Payment!;
            var lease = r.Lease;

            listingsById.TryGetValue(lease.ListingId, out var listing);
            PropertyAggregate? property = null;
            if (listing is not null)
            {
                propertiesById.TryGetValue(listing.PropertyId, out property);
            }

            var locationParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(property?.PropertyAddress.Area))
                locationParts.Add(property.PropertyAddress.Area);
            if (!string.IsNullOrWhiteSpace(property?.PropertyAddress.City))
                locationParts.Add(property.PropertyAddress.City);

            var payoutAmount = payment.Split?.LandlordPayout.Amount ?? payment.GrossAmount.Amount;
            var currency = payment.Split?.LandlordPayout.Currency ?? payment.GrossAmount.Currency;

            var updatedAt = payment.PayoutFailedAt
                ?? payment.PayoutStartedAt
                ?? payment.CreatedAt;

            return new AdminPayoutItem(
                lease.Id,
                lease.ListingId,
                listing?.Title ?? "(property removed)",
                string.Join(", ", locationParts),
                DisplayName(usersById.GetValueOrDefault(lease.LandlordUserId)),
                DisplayName(usersById.GetValueOrDefault(lease.TenantUserId)),
                payoutAmount,
                currency,
                payment.Status,
                payment.PayoutAttempts,
                payment.PayoutFailureReason,
                payment.PayoutFailedAt,
                payment.PayoutStartedAt,
                payment.PayoutReference,
                payment.Status == EscrowStatus.PayoutFailed,
                updatedAt);
        }).ToList();

        return Result<PagedResult<AdminPayoutItem>>.Ok(
            new PagedResult<AdminPayoutItem>(request.Page, request.PageSize, total, items));
    }

    private static string DisplayName(UserAggregate? user)
    {
        if (user is null)
        {
            return string.Empty;
        }

        var name = string.Join(" ", user.FirstName, user.LastName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email.Value : name;
    }
}
