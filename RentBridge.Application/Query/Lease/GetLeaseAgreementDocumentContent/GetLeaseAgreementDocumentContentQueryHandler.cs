using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Services;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseAgreementDocumentContentQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    ILogger<GetLeaseAgreementDocumentContentQueryHandler> logger)
    : IRequestHandler<GetLeaseAgreementDocumentContentQuery, Result<AgreementDocumentContent>>
{
    public async Task<Result<AgreementDocumentContent>> Handle(
        GetLeaseAgreementDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<AgreementDocumentContent>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<AgreementDocumentContent>.Fail("Lease not found");
        }

        LeaseParty? party = null;
        if (user.Id == lease.TenantUserId) party = LeaseParty.Tenant;
        else if (user.Id == lease.LandlordUserId) party = LeaseParty.Landlord;

        var isAssignedLawyer = user.Id == lease.AssignedLawyerId;
        var isAdmin = user.Role is UserRole.Admin;
        var isLawyerOrAdmin = isAssignedLawyer || isAdmin;

        if (party is null && !isLawyerOrAdmin)
        {
            logger.LogInformation("User {UserId} does not have access to lease {LeaseId}", user.Id, request.LeaseId);
            return Result<AgreementDocumentContent>.Forbid("You do not have access to this agreement.");
        }

        var resolved = AgreementDocumentResolver.Resolve(lease, isLawyerOrAdmin, party);
        if (resolved is null)
        {
            return Result<AgreementDocumentContent>.Fail("The agreement document is not available yet.");
        }

        // Record that a party opened their copy — but only while the flow is live
        // and only for the party-facing steps; the lawyer's review opens do not
        // count as a party download and the final copy needs no stamp.
        if (party is not null && !resolved.IsFinal)
        {
            var mark = lease.MarkAgreementDocumentDownloaded(party.Value);
            if (mark.IsSuccess)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var file = await fileStorage.DownloadAsync(resolved.Url, cancellationToken);
        if (!file.IsSuccess)
        {
            logger.LogWarning("Could not fetch agreement document for lease {LeaseId}: {Error}", request.LeaseId, file.Error);
            return Result<AgreementDocumentContent>.Fail("The agreement document could not be retrieved.");
        }

        return Result<AgreementDocumentContent>.Ok(new AgreementDocumentContent(
            file.Value.Content,
            file.Value.ContentType,
            resolved.FileName ?? file.Value.FileName));
    }
}
