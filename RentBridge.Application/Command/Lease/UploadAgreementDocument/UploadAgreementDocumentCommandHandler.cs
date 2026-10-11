using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

public sealed class UploadAgreementDocumentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAgreementFileCleanupService fileCleanup,
    ILogger<UploadAgreementDocumentCommandHandler> logger)
    : IRequestHandler<UploadAgreementDocumentCommand, Result>
{
    public async Task<Result> Handle(UploadAgreementDocumentCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        if (user.Role is not (UserRole.Lawyer or UserRole.Admin))
        {
            logger.LogInformation("User {UserId} is not authorized to upload agreement documents", user.Id);
            return Result.Forbid("Only a lawyer or admin can upload the agreement document.");
        }

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        if (user.Role is UserRole.Lawyer && user.Id != lease.AssignedLawyerId)
        {
            logger.LogInformation("User {UserId} is not the assigned lawyer for lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Forbid("Only the assigned lawyer can upload the agreement document.");
        }

        var result = lease.UploadAgreementDocument(request.DocumentUrl, request.FileName, user.Id);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot accept agreement document: {Error}", request.LeaseId, result.Error);
            return Result.Fail(result.Error!);
        }

        // A brand-new AgreementFile added to an already-tracked Agreement is picked
        // up as Modified, so EF would UPDATE a row that was never inserted.
        unitOfWork.MarkAsAdded(result.Value);

        // Replacing the source document supersedes the previous one; reclaim its
        // bytes now. Best-effort: the periodic sweep retries anything left behind.
        await fileCleanup.PurgePendingDeletionsAsync(lease, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
