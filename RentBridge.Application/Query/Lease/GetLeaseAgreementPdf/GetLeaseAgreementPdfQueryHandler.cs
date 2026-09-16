using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseAgreementPdfQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAgreementDocumentService agreementService,
    IAgreementPdfRenderer pdfRenderer,
    ILogger<GetLeaseAgreementPdfQueryHandler> logger)
    : IRequestHandler<GetLeaseAgreementPdfQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GetLeaseAgreementPdfQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<byte[]>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<byte[]>.Fail("Lease not found");
        }

        var isParty = user.Id == lease.LandlordUserId || user.Id == lease.TenantUserId;
        var isAssignedLawyer = user.Id == lease.AssignedLawyerId;
        var isAdmin = user.Role is UserRole.Admin;
        if (!isParty && !isAssignedLawyer && !isAdmin)
        {
            logger.LogInformation("User {UserId} does not have access to lease {LeaseId}", user.Id, request.LeaseId);
            return Result<byte[]>.Fail("You do not have access to this lease.");
        }

        var composed = await agreementService.EnsureComposedAsync(lease, cancellationToken);
        if (!composed.IsSuccess)
        {
            return Result<byte[]>.Fail(composed.Error!);
        }

        var pdf = pdfRenderer.Render(
            composed.Value,
            lease.Agreement.IsCertified,
            lease.Agreement.CertifyingLawyerId,
            lease.Agreement.CertifiedAt,
            lease.Agreement.Signatures);

        return Result<byte[]>.Ok(pdf);
    }
}