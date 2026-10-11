using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Services;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Returns a short-lived signed URL for the agreement document the caller may
/// see. Accessible to the landlord, tenant, assigned lawyer and admins; the
/// lawyer/admin review path and the finalized final document are covered by the
/// shared resolver so this can never expose a document the caller should not get.
/// </summary>
public sealed class DownloadAgreementDocumentCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    ILogger<DownloadAgreementDocumentCommandHandler> logger)
    : IRequestHandler<DownloadAgreementDocumentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DownloadAgreementDocumentCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<string>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<string>.Fail("Lease not found");
        }

        LeaseParty? party = null;
        if (user.Id == lease.TenantUserId) party = LeaseParty.Tenant;
        else if (user.Id == lease.LandlordUserId) party = LeaseParty.Landlord;

        var isAssignedLawyer = user.Id == lease.AssignedLawyerId;
        var isAdmin = user.Role is UserRole.Admin;
        var isLawyerOrAdmin = isAssignedLawyer || isAdmin;

        if (party is null && !isLawyerOrAdmin)
        {
            logger.LogInformation("User {UserId} is not permitted to download agreement for lease {LeaseId}", user.Id, request.LeaseId);
            return Result<string>.Forbid("You do not have access to this agreement.");
        }

        var resolved = AgreementDocumentResolver.Resolve(lease, isLawyerOrAdmin, party);
        if (resolved is null)
        {
            return Result<string>.Fail(party == LeaseParty.Tenant && !isLawyerOrAdmin
                ? "The landlord's signed agreement is not available yet."
                : "The agreement document has not been uploaded yet.");
        }

        if (party is not null && !resolved.IsFinal)
        {
            var mark = lease.MarkAgreementDocumentDownloaded(party.Value);
            if (!mark.IsSuccess)
            {
                return Result<string>.Fail(mark.Error!);
            }
        }

        var signed = await fileStorage.GetSignedDownloadUrlAsync(resolved.Url, cancellationToken);
        if (!signed.IsSuccess)
        {
            return Result<string>.Fail(signed.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<string>.Ok(signed.Value);
    }
}
