using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using ListingAggregate = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// One-off maintenance sweep for records orphaned before property deletes cascaded.
/// A lease is purged only when nothing financial is attached to it; anything that moved
/// money is reported and left untouched so the accounting trail is never destroyed.
/// Runs as a dry run unless <c>Execute</c> is set.
/// </summary>
public sealed record PurgeOrphanedRecordsCommand(bool Execute)
    : IRequest<Result<OrphanPurgeReport>>;

public sealed record OrphanPurgeReport(
    int OrphanLeasesFound,
    int OrphanListingsFound,
    int LeasesProtectedByMoney,
    int LeasesPurged,
    int ListingsPurged,
    bool Executed,
    IReadOnlyList<Guid> ProtectedLeaseIds);

public sealed class PurgeOrphanedRecordsCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<PurgeOrphanedRecordsCommandHandler> logger)
    : IRequestHandler<PurgeOrphanedRecordsCommand, Result<OrphanPurgeReport>>
{
    /// <summary>Any escrow that touched real money: held, in flight, paid out or refunded.</summary>
    private static readonly EscrowStatus[] MoneyBearingStatuses =
    [
        EscrowStatus.Funded,
        EscrowStatus.Releasing,
        EscrowStatus.PayoutFailed,
        EscrowStatus.Released,
        EscrowStatus.Refunded,
    ];

    public async Task<Result<OrphanPurgeReport>> Handle(
        PurgeOrphanedRecordsCommand request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (res.IsSuccess is false)
        {
            return Result<OrphanPurgeReport>.Fail(res.Error!);
        }

        var actor = res.Value;
        if (actor.Role != UserRole.Admin)
        {
            logger.LogInformation(
                "User {UserId} attempted the orphan purge without admin role",
                actor.Id);
            return Result<OrphanPurgeReport>.Forbid("Only an admin can purge orphaned records.");
        }

        var orphanLeaseIds = await unitOfWork.Leases.GetIdsWithoutLiveListingAsync(cancellationToken);
        var orphanListingIds = await unitOfWork.Listings.GetIdsWithoutLivePropertyAsync(cancellationToken);

        var leases = await unitOfWork.Leases.GetTrackedByIdsWithGraphAsync(orphanLeaseIds, cancellationToken);

        // A lease with escrow that moved money, or with any ledger line at all, is
        // financially material: report it and keep it. Only pre-money orphans qualify.
        var moneyTouched = (await unitOfWork.Ledger.GetLeaseIdsWithLedgerEntriesAsync(
            orphanLeaseIds,
            cancellationToken)).ToHashSet();

        var protectedIds = new List<Guid>();
        var purgeableLeases = new List<LeaseAggregate>();

        foreach (var lease in leases)
        {
            var hasMoney = lease.EscrowPayments.Any(p => MoneyBearingStatuses.Contains(p.Status));
            if (hasMoney || moneyTouched.Contains(lease.Id))
            {
                protectedIds.Add(lease.Id);
            }
            else
            {
                purgeableLeases.Add(lease);
            }
        }

        var report = new OrphanPurgeReport(
            OrphanLeasesFound: orphanLeaseIds.Count,
            OrphanListingsFound: orphanListingIds.Count,
            LeasesProtectedByMoney: protectedIds.Count,
            LeasesPurged: 0,
            ListingsPurged: 0,
            Executed: false,
            ProtectedLeaseIds: protectedIds);

        if (request.Execute is false)
        {
            logger.LogWarning(
                "Orphan purge DRY RUN: {LeaseCount} orphan lease(s) ({Purgeable} purgeable, {Protected} protected by money), {ListingCount} orphan listing(s)",
                report.OrphanLeasesFound,
                purgeableLeases.Count,
                report.LeasesProtectedByMoney,
                report.OrphanListingsFound);
            return Result<OrphanPurgeReport>.Ok(report);
        }

        foreach (var lease in purgeableLeases)
        {
            unitOfWork.Repository<LeaseAggregate>().Remove(lease);
        }

        var purgeableListings = await unitOfWork.Listings.GetTrackedByIdsAsync(
            orphanListingIds,
            cancellationToken);
        foreach (var listing in purgeableListings)
        {
            unitOfWork.Repository<ListingAggregate>().Remove(listing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Orphan purge EXECUTED by {UserId}: purged {LeaseCount} lease(s) and {ListingCount} listing(s); kept {Protected} lease(s) with money attached",
            actor.Id,
            purgeableLeases.Count,
            purgeableListings.Count,
            protectedIds.Count);

        return Result<OrphanPurgeReport>.Ok(report with
        {
            LeasesPurged = purgeableLeases.Count,
            ListingsPurged = purgeableListings.Count,
            Executed = true,
        });
    }
}