using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Lease;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetLeaseQueryHandler> logger)
    : IRequestHandler<GetLeaseQuery, Result<LeaseDetailResponse>>
{
    public async Task<Result<LeaseDetailResponse>> Handle(GetLeaseQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<LeaseDetailResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<LeaseDetailResponse>.Fail("Lease not found");
        }

        var isParty = user.Id == lease.LandlordUserId || user.Id == lease.TenantUserId;
        var isAssignedLawyer = user.Id == lease.AssignedLawyerId;
        var isAdmin = user.Role is UserRole.Admin;
        if (!isParty && !isAssignedLawyer && !isAdmin)
        {
            logger.LogInformation("User {UserId} does not have access to lease {LeaseId}", user.Id, request.LeaseId);
            return Result<LeaseDetailResponse>.Fail("You do not have access to this lease.");
        }

        var detail = new LeaseDetailResponse(
            lease.Id,
            lease.ListingId,
            lease.TenantUserId,
            lease.LandlordUserId,
            lease.AssignedLawyerId,
            lease.Status,
            lease.CreatedAt,
            new AgreementDetail(
                lease.Agreement.IsCertified,
                lease.Agreement.CertifyingLawyerId,
                lease.Agreement.CertifiedAt,
                lease.Agreement.IsFullySigned,
                lease.Agreement.Signatures
                    .Select(s => new SignatureItem(s.Party, s.SignedAt))
                    .ToList()),
            lease.EscrowPayments
                .Select(p => new EscrowPaymentItem(
                    p.Id,
                    p.UserId,
                    p.GrossAmount.Amount,
                    p.GrossAmount.Currency,
                    p.Status,
                    p.CreatedAt))
                .ToList());

        return Result<LeaseDetailResponse>.Ok(detail);
    }
}