using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

public sealed class ApproveAgreementDocumentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILawyerAssignmentService lawyerService,
    IAgreementFileCleanupService fileCleanup,
    ILogger<ApproveAgreementDocumentCommandHandler> logger)
    : IRequestHandler<ApproveAgreementDocumentCommand, Result>
{
    public async Task<Result> Handle(ApproveAgreementDocumentCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {UserId} is not authorized to approve agreement documents", user.Id);
            return Result.Forbid("Only a lawyer or admin can approve the agreement document.");
        }

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        // A lawyer can be suspended or rejected after assignment, so verification
        // is re-checked here — same defensive rule as the certify path. An admin
        // may approve on the assigned lawyer's behalf without the re-check.
        if (user.Role is UserRole.Lawyer)
        {
            if (lease.AssignedLawyerId is null)
            {
                return Result.Fail("No lawyer is assigned to this lease yet.");
            }

            if (!await lawyerService.IsVerifiedLawyerAsync(lease.AssignedLawyerId.Value, cancellationToken))
            {
                logger.LogWarning("Assigned lawyer {LawyerId} for lease {LeaseId} is no longer verified", lease.AssignedLawyerId, request.LeaseId);
                return Result.Fail("The assigned lawyer is no longer verified. Contact an admin.");
            }
        }

        var approverId = user.Role is UserRole.Admin && lease.AssignedLawyerId is not null
            ? lease.AssignedLawyerId.Value
            : user.Id;

        var result = lease.ApproveAgreementDocument(request.Party, approverId);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot approve {Party} document: {Error}", request.LeaseId, request.Party, result.Error);
            return result;
        }

        // Approving the tenant finalizes the flow and flags the lawyer's source
        // document plus the landlord's signed copy for deletion. Reclaim their
        // bytes now; anything that fails is retried by the periodic sweep.
        await fileCleanup.PurgePendingDeletionsAsync(lease, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
