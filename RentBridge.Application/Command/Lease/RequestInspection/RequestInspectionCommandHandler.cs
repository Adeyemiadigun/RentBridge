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
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
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
                    return Result.Fail("Only the tenant on this lease can request an inspection");
                }

                var result = lease.RequestInspection(user.Id, request.PreferredDate, request.Note);
                if (!result.IsSuccess)
                {
                    logger.LogInformation("Lease {leaseId} cannot accept an inspection request: {error}", request.LeaseId, result.Error);
                    return Result.Fail(result.Error!);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxRetryAttempts - 1)
            {
                logger.LogWarning("Concurrency conflict on lease {leaseId}, attempt {attempt}/{maxAttempts}", request.LeaseId, attempt + 1, MaxRetryAttempts);
                // Wait a bit before retrying with exponential backoff
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