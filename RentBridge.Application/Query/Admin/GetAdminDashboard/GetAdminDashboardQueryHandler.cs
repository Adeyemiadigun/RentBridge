using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Query.Admin;

public sealed class GetAdminDashboardQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetAdminDashboardQueryHandler> logger)
    : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardResponse>>
{
    public async Task<Result<AdminDashboardResponse>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<AdminDashboardResponse>.Fail(res.Error!);
        }
        var actor = res.Value;

        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation("User {UserId} attempted to view the admin dashboard without admin role", actor.Id);
            return Result<AdminDashboardResponse>.Fail("Only an admin can view the dashboard.");
        }

        var users = unitOfWork.Repository<UserAggregate>();
        // Single GROUP BY per entity — no entity (and no owned-collection)
        // hydration. Absent buckets mean zero; Total is the bucket sum.
        var roleCounts = (await users.CountByAsync(null, u => u.Role, cancellationToken))
            .ToDictionary(r => r.Key, r => r.Count);
        var userSummary = new UserSummary(
            Total: roleCounts.Values.Sum(),
            Tenants: roleCounts.GetValueOrDefault(UserRole.Tenant),
            Landlords: roleCounts.GetValueOrDefault(UserRole.Landlord),
            Agents: roleCounts.GetValueOrDefault(UserRole.Agent),
            Caretakers: roleCounts.GetValueOrDefault(UserRole.Caretaker),
            Lawyers: roleCounts.GetValueOrDefault(UserRole.Lawyer),
            IdentityVerified: await users.CountAsync(u => u.IdentityVerified, cancellationToken),
            PendingLawyers: await users.CountAsync(
                u => u.Role == UserRole.Lawyer
                    && u.LawyerProfile != null
                    && u.LawyerProfile.Status == LawyerStatus.Pending,
                cancellationToken));

        var listings = unitOfWork.Repository<ListingAggregate>();
        var listingCounts = (await listings.CountByAsync(null, l => l.Status, cancellationToken))
            .ToDictionary(r => r.Key, r => r.Count);
        var listingSummary = new ListingSummary(
            Total: listingCounts.Values.Sum(),
            Draft: listingCounts.GetValueOrDefault(ListingStatus.Draft),
            Published: listingCounts.GetValueOrDefault(ListingStatus.Published),
            Unpublished: listingCounts.GetValueOrDefault(ListingStatus.Unpublished),
            Closed: listingCounts.GetValueOrDefault(ListingStatus.Closed));

        var leases = unitOfWork.Repository<LeaseAggregate>();
        var leaseCounts = (await leases.CountByAsync(null, l => l.Status, cancellationToken))
            .ToDictionary(r => r.Key, r => r.Count);
        var leaseSummary = new LeaseSummary(
            Total: leaseCounts.Values.Sum(),
            FundedInEscrow: leaseCounts.GetValueOrDefault(LeaseStatus.FundedInEscrow),
            Releasing: leaseCounts.GetValueOrDefault(LeaseStatus.Releasing),
            Released: leaseCounts.GetValueOrDefault(LeaseStatus.Released));

        var totals = await unitOfWork.Ledger.GetTotalsAsync(null, cancellationToken);

        // Money held between funding and payout, derived from ledger lines:
        // every funding writes an EscrowFunded credit; every release writes
        // commission + legal debits and a payout credit summing back to gross.
        var funded = totals
            .Where(t => t.Type == TransactionType.EscrowFunded)
            .Sum(t => t.Total);
        var released = totals
            .Where(t => t.Type is TransactionType.PlatformCommission
                or TransactionType.LegalFeeShare
                or TransactionType.LandlordPayout)
            .Sum(t => t.Total);
        var inFlight = new EscrowInFlight(
            leaseCounts.GetValueOrDefault(LeaseStatus.FundedInEscrow)
                + leaseCounts.GetValueOrDefault(LeaseStatus.Releasing),
            funded - released,
            totals.Select(t => t.Currency).FirstOrDefault() ?? string.Empty);

        return Result<AdminDashboardResponse>.Ok(
            new AdminDashboardResponse(userSummary, listingSummary, leaseSummary, inFlight, new LedgerMetrics(totals)));
    }
}
