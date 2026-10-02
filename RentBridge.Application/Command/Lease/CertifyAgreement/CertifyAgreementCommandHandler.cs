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
            return Result.Forbid("Only a lawyer or admin can certify an agreement.");
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

        // A lawyer can be suspended or rejected after being assigned, so Verified
        // must be re-checked here, not only at pick time. Mirrors the property
        // review path (ResolveAndAuthorizeAsync): re-pick and reassign instead of
        // dead-ending the lease. There is no lease-reassign endpoint, and
        // AssignLawyer raises LawyerAssigned, which emails the new lawyer.
        if (!await lawyerService.IsVerifiedLawyerAsync(lease.AssignedLawyerId.Value, cancellationToken))
        {
            var previous = lease.AssignedLawyerId.Value;
            var picked = await lawyerService.PickNextVerifiedLawyerAsync(cancellationToken);
            if (!picked.IsSuccess)
            {
                logger.LogWarning("Lease {LeaseId} lawyer {LawyerId} is no longer verified and no verified lawyer is available", request.LeaseId, previous);
                return Result.Fail("The assigned lawyer is no longer verified and no verified lawyer is currently available. Contact an admin.");
            }

            var reassign = lease.AssignLawyer(picked.Value);
            if (!reassign.IsSuccess)
                return Result.Fail(reassign.Error!);

            logger.LogWarning("Reassigned lease {LeaseId} lawyer {Previous} -> {Next}: previous lawyer no longer verified", request.LeaseId, previous, picked.Value);

            // Persist the reassignment even though this request fails, otherwise the
            // lease stays wedged in LegalReview with no recovery path.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Fail("The previously assigned lawyer is no longer verified. This lease has been reassigned and must be certified by the new assigned lawyer.");
        }

        if (user.Id != lease.AssignedLawyerId)
        {
            logger.LogInformation("User {UserId} is not the assigned lawyer for lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Forbid("Only the assigned lawyer can certify this agreement.");
        }

        var certify = lease.Certify(user.Id);
        if (!certify.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot be certified: {Error}", request.LeaseId, certify.Error);
            return Result.Fail(certify.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // No escrow-release attempt here. Certification closes the legal gate, but
        // the parties still have to sign and the tenant still has to fund escrow.
        // Money moves only from the funding-success webhook.

        return Result.Ok();
    }
}