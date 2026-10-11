using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public sealed class SubmitAgreementDocumentForReviewCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SubmitAgreementDocumentForReviewCommandHandler> logger)
    : IRequestHandler<SubmitAgreementDocumentForReviewCommand, Result>
{
    public async Task<Result> Handle(SubmitAgreementDocumentForReviewCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        if (!LeasePartyResolver.TryResolve(user.Id, lease, out var party))
        {
            logger.LogInformation("User {UserId} is not a party to lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Forbid("Only the tenant or landlord can submit a signed agreement for review.");
        }

        var result = lease.SubmitSignedAgreementDocumentForReview(party);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot submit {Party} document: {Error}", request.LeaseId, party, result.Error);
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
