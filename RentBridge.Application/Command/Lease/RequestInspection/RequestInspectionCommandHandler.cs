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
                var lease = await unitOfWork.Leases.GetWithInspectionRequestsAsync(request.LeaseId, cancellationToken);
                if (lease is null)
                {
                    return Result.Fail("Lease not found");
                }

                if (lease.TenantUserId != user.Id)
                {
                    return Result.Fail("Only the tenant on this lease can request an inspection");
                }

                var result = lease.RequestInspection(user.Id, request.PreferredDate, request.Note);
                if (!result.IsSuccess)
                {
                    return Result.Fail(result.Error!);
                }

                // A brand-new InspectionRequest added to an already-tracked Lease is picked up
                // as Modified, so EF would UPDATE a row that was never inserted.
                if (result.Value is not null)
                {
                    unitOfWork.MarkAsAdded(result.Value);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Inspection requested for lease {leaseId} on {preferredDate}",
                    request.LeaseId, request.PreferredDate);

                return Result.Ok();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxRetryAttempts - 1)
            {
                // Clear change tracker to force fresh load on retry
                unitOfWork.ClearChangeTracker();
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
                continue;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Result.Fail("The lease was modified by another process. Please try again.");
            }
        }

        return Result.Fail("The lease was modified by another process. Please try again.");
    }
}
