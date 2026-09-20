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

        var ownListings = await unitOfWork.Repository<ListingAggregate>()
            .FindAsync(l => l.OwnerUserId == user.Id, cancellationToken);
        var listingSummary = new ListingSummary(
            Total: ownListings.Count,
            Draft: ownListings.Count(l => l.Status == ListingStatus.Draft),
            Published: ownListings.Count(l => l.Status == ListingStatus.Published),
            Unpublished: ownListings.Count(l => l.Status == ListingStatus.Unpublished),
            Closed: ownListings.Count(l => l.Status == ListingStatus.Closed));

        var ownLeases = await unitOfWork.Repository<LeaseAggregate>()
            .FindAsync(l => l.LandlordUserId == user.Id, cancellationToken);
        var leaseSummary = new LeaseSummary(
            Total: ownLeases.Count,
            FundedInEscrow: ownLeases.Count(l => l.Status == LeaseStatus.FundedInEscrow),
            Releasing: ownLeases.Count(l => l.Status == LeaseStatus.Releasing),
            Released: ownLeases.Count(l => l.Status == LeaseStatus.Released));

        var heldPayments = ownLeases
            .Where(l => l.Status is LeaseStatus.FundedInEscrow or LeaseStatus.Releasing)
            .SelectMany(l => l.EscrowPayments)
            .Where(p => p.Status is EscrowStatus.Funded or EscrowStatus.Releasing or EscrowStatus.PayoutFailed)
            .ToList();
        var inFlight = new EscrowInFlight(
            ownLeases.Count(l => l.Status is LeaseStatus.FundedInEscrow or LeaseStatus.Releasing),
            heldPayments.Sum(p => p.GrossAmount.Amount),
            heldPayments.Select(p => p.GrossAmount.Currency).FirstOrDefault() ?? string.Empty);

        var payoutTotals = await unitOfWork.Ledger.GetTotalsAsync(
            e => e.LandlordUserId == user.Id, cancellationToken);

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
