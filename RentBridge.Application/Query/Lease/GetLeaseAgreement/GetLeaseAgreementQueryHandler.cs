using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseAgreementQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAgreementDocumentService agreementService,
    ILogger<GetLeaseAgreementQueryHandler> logger)
    : IRequestHandler<GetLeaseAgreementQuery, Result<LeaseAgreementResponse>>
{
    public async Task<Result<LeaseAgreementResponse>> Handle(GetLeaseAgreementQuery request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<LeaseAgreementResponse>.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<LeaseAgreementResponse>.Fail("Lease not found");
        }

        if (!HasAccess(user.Id, user.Role, lease))
        {
            logger.LogInformation("User {UserId} does not have access to lease {LeaseId}", user.Id, request.LeaseId);
            return Result<LeaseAgreementResponse>.Fail("You do not have access to this lease.");
        }

        var composed = await agreementService.EnsureComposedAsync(lease, cancellationToken);
        if (!composed.IsSuccess)
        {
            return Result<LeaseAgreementResponse>.Fail(composed.Error!);
        }
        var document = composed.Value;

        var terms = JsonSerializer.Deserialize<AgreementTerms>(document.TermsJson)
            ?? throw new InvalidOperationException("Stored agreement terms could not be deserialized.");

        var response = new LeaseAgreementResponse(
            document.Version,
            document.ContentHash,
            document.DraftedAt,
            terms,
            lease.Agreement.IsCertified,
            lease.Agreement.IsFullySigned,
            lease.Agreement.CertifyingLawyerId,
            lease.Agreement.CertifiedAt,
            lease.Agreement.Signatures
                .Select(s => new SignatureItem(s.Party, s.SignedAt))
                .ToList());

        return Result<LeaseAgreementResponse>.Ok(response);
    }

    private static bool HasAccess(Guid userId, UserRole role, LeaseAggregate lease)
        => userId == lease.LandlordUserId
           || userId == lease.TenantUserId
           || userId == lease.AssignedLawyerId
           || role is UserRole.Admin;
}