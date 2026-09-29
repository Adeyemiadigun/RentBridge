using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

public sealed class CertifyAgreementCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    IAgreementDocumentService agreementService,
    IEscrowReleaseService releaseService,
    ILogger<CertifyAgreementCommandHandler> logger)
    : IRequestHandler<CertifyAgreementCommand, Result>
{
    public async Task<Result> Handle(CertifyAgreementCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {UserId} is not authorized to certify agreements", user.Id);
            return Result.Fail("Only a lawyer or admin can certify an agreement.");
        }

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        var composed = await agreementService.EnsureComposedAsync(lease, cancellationToken);
        if (!composed.IsSuccess)
        {
            logger.LogWarning("Cannot compose agreement for lease {LeaseId}: {Error}", request.LeaseId, composed.Error);
            return Result.Fail(composed.Error!);
        }

        // No lazy pick here: a round-robin pick can never be the caller, so it
        // would only consume a slot and then fail the ownership check below.
        if (lease.AssignedLawyerId is null)
        {
            logger.LogInformation("Lease {LeaseId} has no assigned lawyer; cannot certify", request.LeaseId);
            return Result.Fail("No lawyer is assigned to this lease yet.");
        }

        if (user.Id != lease.AssignedLawyerId)
        {
            logger.LogInformation("User {UserId} is not the assigned lawyer for lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the assigned lawyer can certify this agreement.");
        }

        var certify = lease.Certify(user.Id);
        if (!certify.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot be certified: {Error}", request.LeaseId, certify.Error);
            return Result.Fail(certify.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Legal review is one of the three release gates; if escrow is already
        // funded and this was the last gate, the payout runs now.
        await releaseService.TryAutoReleaseAsync(lease.Id, cancellationToken);

        return Result.Ok();
    }
}