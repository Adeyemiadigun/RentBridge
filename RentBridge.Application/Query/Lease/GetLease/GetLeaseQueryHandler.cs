using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Lease;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using UserAggregate = RentBridge.Domain.Aggregates.User;
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

        // Must use the graph loader, not a bare query: this handler maps the
        // signatures, the document and the escrow payments, and EF leaves all three
        // empty unless they are explicitly loaded.
        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
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

        // Cheap canary for the failure mode where an owned collection comes back
        // empty: a lease whose status claims signatures are in progress but which
        // reports none means a loader regressed, not that the user did nothing.
        if (lease.Status is LeaseStatus.Certified or LeaseStatus.PartiallySigned or LeaseStatus.FullySigned
            && lease.Agreement.Signatures.Count == 0)
        {
            logger.LogWarning(
                "Lease {LeaseId} is {Status} but loaded with zero signatures — an owned collection was not loaded",
                lease.Id,
                lease.Status);
        }

        // Calculate total amount (gross + caution fee + real house fee + agent fee) from the first escrow payment's split
        decimal? totalAmount = null;
        string? totalAmountCurrency = null;
        var firstPayment = lease.EscrowPayments.FirstOrDefault();
        if (firstPayment is not null && firstPayment.Split is not null)
        {
            totalAmount = firstPayment.GrossAmount.Amount;
            totalAmountCurrency = firstPayment.GrossAmount.Currency;
        }

        // The assigned lawyer's bar number is only on the User's owned
        // LawyerProfile, so it has to be loaded separately and projected.
        var lawyer = lease.AssignedLawyerId is Guid assignedLawyerId
            ? await unitOfWork.Repository<UserAggregate>()
                .FirstOrDefault(u => u.Id == assignedLawyerId, cancellationToken)
            : null;

        var assignedLawyer = lawyer is null
            ? null
            : new AssignedLawyerSummary(
                lawyer.Id,
                $"{lawyer.FirstName} {lawyer.LastName}".Trim(),
                lawyer.Email.Value,
                lawyer.Phone.Value,
                lawyer.LawyerProfile?.BarNumber);

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
            lease.Agreement.Document is null
                ? null
                : new AgreementDocumentItem(
                    lease.Agreement.Document.Version,
                    lease.Agreement.Document.ContentHash,
                    lease.Agreement.Document.DraftedAt),
            lease.EscrowPayments
                .Select(p => new EscrowPaymentItem(
                    p.Id,
                    p.UserId,
                    p.GrossAmount.Amount,
                    p.GrossAmount.Currency,
                    p.Status,
                    p.CreatedAt,
                    p.GrossAmount.Amount, // Total amount (same as gross for now, split is calculated from gross)
                    p.GrossAmount.Currency))
                .ToList(),
            assignedLawyer,
            totalAmount,
            totalAmountCurrency);

        return Result<LeaseDetailResponse>.Ok(detail);
    }
}