using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class RequestInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RequestInspectionCommandHandler> logger)
    : IRequestHandler<RequestInspectionCommand, Result>
{
    private const int MaxRetryAttempts = 3;

    public async Task<Result> Handle(RequestInspectionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEBUG] RequestInspectionCommand received for LeaseId: {leaseId}", request.LeaseId);
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            logger.LogWarning("[DEBUG] GetCurrentUser failed: {error}", res.Error);
            return Result.Fail(res.Error!);
        }
        var user = res.Value;
        logger.LogInformation("[DEBUG] Current user: {userId}, Role: {role}", user.Id, user.Role);

        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("[DEBUG] Attempt {attempt}: Loading lease {leaseId} with inspection requests", attempt + 1, request.LeaseId);
                var lease = await unitOfWork.Leases.GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
                if (lease is null)
                {
                    logger.LogInformation("[DEBUG] Lease {leaseId} not found", request.LeaseId);
                    return Result.Fail("Lease not found");
                }

                // Log Version/xmin for debugging concurrency (shadow property via reflection)
                var versionVal = lease.GetType().GetProperty("Version")?.GetValue(lease) ?? "unknown";
                logger.LogInformation("[DEBUG] Lease Version: {version}", versionVal);
                foreach (var ir in lease.InspectionRequests)
                {
                    logger.LogInformation("[DEBUG] InspectionRequest: Id={id}, Status={status}, PreferredDate={preferredDate}", ir.Id, ir.Status, ir.PreferredDate);
                }

                if (lease.TenantUserId != user.Id)
                {
                    logger.LogInformation("[DEBUG] User {userId} is not the tenant of lease {leaseId}", user.Id, request.LeaseId);
                    return Result.Fail("Only the tenant on this lease can request an inspection");
                }

                var result = lease.RequestInspection(user.Id, request.PreferredDate, request.Note);
                if (!result.IsSuccess)
                {
                    logger.LogInformation("[DEBUG] Lease {leaseId} cannot accept an inspection request: {error}", request.LeaseId, result.Error);
                    return Result.Fail(result.Error!);
                }

                logger.LogInformation("[DEBUG] Saving changes...");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("[DEBUG] SaveChanges succeeded");
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxRetryAttempts - 1)
            {
                logger.LogWarning("[DEBUG] Concurrency conflict on lease {leaseId}, attempt {attempt}/{maxAttempts}", request.LeaseId, attempt + 1, MaxRetryAttempts);
                // Clear change tracker to force fresh load on retry
                unitOfWork.ClearChangeTracker();
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
                continue;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "[DEBUG] Max retry attempts reached for lease {leaseId}", request.LeaseId);
                return Result.Fail("The lease was modified by another process. Please try again.");
            }
        }

        return Result.Fail("The lease was modified by another process. Please try again.");
    }
}