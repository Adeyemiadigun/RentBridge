using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class DeclineInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<DeclineInspectionCommandHandler> logger)
    : IRequestHandler<DeclineInspectionCommand, Result>
{
    private const int MaxRetryAttempts = 3;

    public async Task<Result> Handle(DeclineInspectionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEBUG] DeclineInspectionCommand received for LeaseId: {leaseId}", request.LeaseId);
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            logger.LogWarning("[DEBUG] GetCurrentUser failed: {error}", res.Error);
            return Result.Fail(res.Error!);
        }
        var user = res.Value;
        logger.LogInformation("[DEBUG] Current user: {userId}, Role: {role}", user.Id, user.Role);

        for (int attempt = 0; attempt < 3; attempt++)
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

                logger.LogInformation("[DEBUG] Lease loaded: Id={id}, Status={status}, LandlordUserId={landlordId}, InspectionRequestsCount={count}", 
                    lease.Id, lease.Status, lease.LandlordUserId, lease.InspectionRequests.Count);
                foreach (var ir in lease.InspectionRequests)
                {
                    logger.LogInformation("[DEBUG] InspectionRequest: Id={id}, Status={status}, PreferredDate={preferredDate}", ir.Id, ir.Status, ir.PreferredDate);
                }

                if (lease.LandlordUserId != user.Id && user.Role != UserRole.Admin)
                {
                    logger.LogInformation("[DEBUG] User {userId} is not authorized to decline inspection for lease {leaseId}", user.Id, request.LeaseId);
                    return Result.Fail("Only the landlord or an admin can decline an inspection");
                }

                var result = lease.DeclinePendingInspection();
                if (!result.IsSuccess)
                {
                    logger.LogInformation("[DEBUG] Lease {leaseId} cannot decline inspection: {error}", request.LeaseId, result.Error);
                    return Result.Fail(result.Error!);
                }

                logger.LogInformation("[DEBUG] Saving changes...");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("[DEBUG] DeclineInspection succeeded");
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < 2)
            {
                logger.LogWarning("[DEBUG] Concurrency conflict on lease {leaseId}, attempt {attempt}/3", request.LeaseId, attempt + 1);
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