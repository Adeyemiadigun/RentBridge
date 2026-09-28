using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public class CancelInspectionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<CancelInspectionCommandHandler> logger)
    : IRequestHandler<CancelInspectionCommand, Result>
{
    private const int MaxRetryAttempts = 3;

    public async Task<Result> Handle(CancelInspectionCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var lease = await unitOfWork.Repository<LeaseAggregate>().GetByIdAsync(request.LeaseId, cancellationToken);
                if (lease is null)
                {
                    logger.LogInformation("Lease {leaseId} not found", request.LeaseId);
                    return Result.Fail("Lease not found");
                }

                if (lease.TenantUserId != user.Id)
                {
                    logger.LogInformation("User {userId} is not the tenant of lease {leaseId}", user.Id, request.LeaseId);
                    return Result.Fail("Only the tenant on this lease can cancel an inspection request");
                }

                var result = lease.CancelPendingInspection(user.Id);
                if (!result.IsSuccess)
                {
                    logger.LogInformation("Lease {leaseId} cannot cancel inspection: {error}", request.LeaseId, result.Error);
                    return Result.Fail(result.Error!);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < 2)
            {
                logger.LogWarning("Concurrency conflict on lease {leaseId}, attempt {attempt}/3", request.LeaseId, attempt + 1);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
                continue;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Max retry attempts reached for lease {leaseId}", request.LeaseId);
                return Result.Fail("The lease was modified by another process. Please try again.");
            }
        }

        return Result.Fail("The lease was modified by another process. Please try again.");
    }
}