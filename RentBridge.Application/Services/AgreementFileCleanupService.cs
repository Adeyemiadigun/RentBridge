using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Services;

/// <summary>
/// Deletes superseded agreement files from object storage and purges their rows.
/// Idempotent by construction: it only touches files flagged for deletion,
/// treats an already-missing asset as success, and leaves any file whose bytes
/// could not be deleted flagged so the sweep retries it.
/// </summary>
public sealed class AgreementFileCleanupService(
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage,
    ILogger<AgreementFileCleanupService> logger) : IAgreementFileCleanupService
{
    public async Task<Result> PurgePendingDeletionsAsync(LeaseAggregate lease, CancellationToken cancellationToken)
    {
        // Materialise first: AcknowledgeFileDeleted mutates the collection.
        var pending = lease.Agreement.Files
            .Where(f => f.IsPendingDeletion && !f.IsFinal)
            .ToList();

        if (pending.Count == 0)
        {
            return Result.Ok();
        }

        var failures = new List<string>();
        foreach (var file in pending)
        {
            var deleted = await fileStorage.DeleteAsync(file.Url, cancellationToken);
            if (deleted.IsSuccess)
            {
                lease.Agreement.AcknowledgeFileDeleted(file.Id);
            }
            else
            {
                failures.Add($"{file.Kind}:{file.Id} ({deleted.Error})");
            }
        }

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "Agreement file cleanup for lease {LeaseId} left {Count} file(s) undeleted; will retry: {Failures}",
                lease.Id, failures.Count, string.Join("; ", failures));
            return Result.Fail($"{failures.Count} agreement file(s) could not be deleted.");
        }

        return Result.Ok();
    }

    public async Task ReconcilePendingDeletionsAsync()
    {
        var cancellationToken = CancellationToken.None;

        var leases = await unitOfWork.Leases.GetWithPendingAgreementFileDeletionsAsync(cancellationToken);
        foreach (var lease in leases)
        {
            try
            {
                var result = await PurgePendingDeletionsAsync(lease, cancellationToken);

                // Persist whatever succeeded (row removals) regardless of partial
                // failure; the still-flagged rows keep this lease in the sweep.
                await unitOfWork.SaveChangesAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    logger.LogWarning(
                        "Agreement file cleanup sweep could not fully purge lease {LeaseId}: {Error}",
                        lease.Id, result.Error);
                }
            }
            catch (Exception ex)
            {
                // A failed save can leave the shared context dirty for the rest of
                // the sweep; drop tracked state so the next lease starts clean. Any
                // bytes already deleted are idempotent to re-delete next sweep.
                unitOfWork.ClearChangeTracker();
                logger.LogError(ex, "Agreement file cleanup sweep failed for lease {LeaseId}; retrying next sweep", lease.Id);
            }
        }
    }
}
