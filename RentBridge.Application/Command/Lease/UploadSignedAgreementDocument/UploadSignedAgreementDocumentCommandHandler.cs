using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Lease;

public sealed class UploadSignedAgreementDocumentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAgreementFileCleanupService fileCleanup,
    ILogger<UploadSignedAgreementDocumentCommandHandler> logger)
    : IRequestHandler<UploadSignedAgreementDocumentCommand, Result>
{
    public async Task<Result> Handle(UploadSignedAgreementDocumentCommand request, CancellationToken cancellationToken)
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
            return Result.Forbid("Only the tenant or landlord can upload a signed agreement.");
        }

        var result = lease.UploadSignedAgreementDocument(party, request.DocumentUrl, request.FileName, user.Id);
        if (!result.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot accept {Party} signed document: {Error}", request.LeaseId, party, result.Error);
            return Result.Fail(result.Error!);
        }

        // New AgreementFile on an already-tracked Agreement must be inserted, not
        // updated (EF would otherwise treat the assigned key as existing).
        unitOfWork.MarkAsAdded(result.Value);

        // A re-upload supersedes this party's previous copy; reclaim it. Best-effort.
        await fileCleanup.PurgePendingDeletionsAsync(lease, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
