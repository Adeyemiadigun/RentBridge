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
        var userSummary = new UserSummary(
            Total: await users.CountAsync(null, cancellationToken),
            Tenants: await users.CountAsync(u => u.Role == UserRole.Tenant, cancellationToken),
            Landlords: await users.CountAsync(u => u.Role == UserRole.Landlord, cancellationToken),
            Agents: await users.CountAsync(u => u.Role == UserRole.Agent, cancellationToken),
            Caretakers: await users.CountAsync(u => u.Role == UserRole.Caretaker, cancellationToken),
            Lawyers: await users.CountAsync(u => u.Role == UserRole.Lawyer, cancellationToken),
            IdentityVerified: await users.CountAsync(u => u.IdentityVerified, cancellationToken),
            PendingLawyers: await users.CountAsync(
                u => u.Role == UserRole.Lawyer
                    && u.LawyerProfile != null
                    && u.LawyerProfile.Status == LawyerStatus.Pending,
                cancellationToken));

        var listings = unitOfWork.Repository<ListingAggregate>();
        var listingSummary = new ListingSummary(
            Total: await listings.CountAsync(null, cancellationToken),
            Draft: await listings.CountAsync(l => l.Status == ListingStatus.Draft, cancellationToken),
            Published: await listings.CountAsync(l => l.Status == ListingStatus.Published, cancellationToken),
            Unpublished: await listings.CountAsync(l => l.Status == ListingStatus.Unpublished, cancellationToken),
            Closed: await listings.CountAsync(l => l.Status == ListingStatus.Closed, cancellationToken));

        var leases = unitOfWork.Repository<LeaseAggregate>();
        var leaseSummary = new LeaseSummary(
            Total: await leases.CountAsync(null, cancellationToken),
            FundedInEscrow: await leases.CountAsync(l => l.Status == LeaseStatus.FundedInEscrow, cancellationToken),
            Releasing: await leases.CountAsync(l => l.Status == LeaseStatus.Releasing, cancellationToken),
            Released: await leases.CountAsync(l => l.Status == LeaseStatus.Released, cancellationToken));

        // Money held between funding and payout. Bounded by active escrows.
        var activeEscrows = await leases.FindAsync(
            l => l.Status == LeaseStatus.FundedInEscrow || l.Status == LeaseStatus.Releasing,
            cancellationToken);
        var heldPayments = activeEscrows
            .SelectMany(l => l.EscrowPayments)
            .Where(p => p.Status is EscrowStatus.Funded or EscrowStatus.Releasing or EscrowStatus.PayoutFailed)
            .ToList();
        var inFlight = new EscrowInFlight(
            activeEscrows.Count,
            heldPayments.Sum(p => p.GrossAmount.Amount),
            heldPayments.Select(p => p.GrossAmount.Currency).FirstOrDefault() ?? string.Empty);

        var totals = await unitOfWork.Ledger.GetTotalsAsync(null, cancellationToken);

        return Result<AdminDashboardResponse>.Ok(
            new AdminDashboardResponse(userSummary, listingSummary, leaseSummary, inFlight, new LedgerMetrics(totals)));
    }
}
