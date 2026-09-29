using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Query.Dashboard;

public sealed class GetOwnerDashboardQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetOwnerDashboardQueryHandler> logger)
    : IRequestHandler<GetOwnerDashboardQuery, Result<OwnerDashboardResponse>>
{
    private const int RecentTransactionCount = 5;

    public async Task<Result<OwnerDashboardResponse>> Handle(
        GetOwnerDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<OwnerDashboardResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Landlord or UserRole.Agent or UserRole.Caretaker))
        {
            logger.LogInformation("User {UserId} with role {Role} attempted to view the owner dashboard", user.Id, user.Role);
            return Result<OwnerDashboardResponse>.Fail("Only listing owners can view this dashboard.");
        }

        // Single GROUP BY per entity — no entity (and no owned-collection)
        // hydration. Absent buckets mean zero.
        var listingCounts = (await unitOfWork.Repository<ListingAggregate>()
                .CountByAsync(l => l.OwnerUserId == user.Id, l => l.Status, cancellationToken))
            .ToDictionary(r => r.Key, r => r.Count);
        var listingSummary = new ListingSummary(
            Total: listingCounts.Values.Sum(),
            Draft: listingCounts.GetValueOrDefault(ListingStatus.Draft),
            Published: listingCounts.GetValueOrDefault(ListingStatus.Published),
            Unpublished: listingCounts.GetValueOrDefault(ListingStatus.Unpublished),
            Closed: listingCounts.GetValueOrDefault(ListingStatus.Closed));

        var leaseCounts = (await unitOfWork.Repository<LeaseAggregate>()
                .CountByAsync(l => l.LandlordUserId == user.Id, l => l.Status, cancellationToken))
            .ToDictionary(r => r.Key, r => r.Count);
        var leaseSummary = new LeaseSummary(
            Total: leaseCounts.Values.Sum(),
            FundedInEscrow: leaseCounts.GetValueOrDefault(LeaseStatus.FundedInEscrow),
            Releasing: leaseCounts.GetValueOrDefault(LeaseStatus.Releasing),
            Released: leaseCounts.GetValueOrDefault(LeaseStatus.Released));

        var payoutTotals = await unitOfWork.Ledger.GetTotalsAsync(
            e => e.LandlordUserId == user.Id, cancellationToken);

        // Money held between funding and payout, derived from ledger lines:
        // every funding writes an EscrowFunded credit; only a finalized
        // release writes the commission + legal + payout lines that net it
        // to zero — failed/in-flight payouts stay counted, as before.
        var funded = payoutTotals
            .Where(t => t.Type == TransactionType.EscrowFunded)
            .Sum(t => t.Total);
        var released = payoutTotals
            .Where(t => t.Type is TransactionType.PlatformCommission
                or TransactionType.LegalFeeShare
                or TransactionType.LandlordPayout)
            .Sum(t => t.Total);
        var inFlight = new EscrowInFlight(
            leaseCounts.GetValueOrDefault(LeaseStatus.FundedInEscrow)
                + leaseCounts.GetValueOrDefault(LeaseStatus.Releasing),
            funded - released,
            payoutTotals.Select(t => t.Currency).FirstOrDefault() ?? string.Empty);

        var recent = await unitOfWork.Repository<LedgerEntry>().GetPagedAsync(
            e => e.LandlordUserId == user.Id,
            page: 1,
            pageSize: RecentTransactionCount,
            orderBy: e => e.OccurredAt,
            ascending: false,
            ct: cancellationToken);
        var recentItems = recent.Items
            .Select(e => new TransactionItem(
                e.Id, e.LeaseId, e.EscrowPaymentId, e.Type, e.Direction,
                e.Amount, e.Currency, e.Reference, e.Status, e.Attempt,
                e.Description, e.OccurredAt))
            .ToList();

        return Result<OwnerDashboardResponse>.Ok(
            new OwnerDashboardResponse(listingSummary, leaseSummary, inFlight, new LedgerMetrics(payoutTotals), recentItems));
    }
}
