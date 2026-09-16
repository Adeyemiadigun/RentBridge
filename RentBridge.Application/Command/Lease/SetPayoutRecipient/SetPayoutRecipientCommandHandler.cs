using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

public sealed class SetPayoutRecipientCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SetPayoutRecipientCommandHandler> logger)
    : IRequestHandler<SetPayoutRecipientCommand, Result>
{
    public async Task<Result> Handle(SetPayoutRecipientCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var landlord = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found.");
        }

        if (landlord.Id != lease.LandlordUserId)
        {
            logger.LogInformation("User {UserId} tried to set a payout recipient on a lease they do not own", landlord.Id);
            return Result.Fail("Only the lease landlord can set a payout recipient.");
        }

        var set = lease.SetLandlordPayoutRecipientCode(request.RecipientCode);
        if (!set.IsSuccess)
        {
            return Result.Fail(set.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}