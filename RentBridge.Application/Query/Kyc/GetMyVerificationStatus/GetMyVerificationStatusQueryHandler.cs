using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Kyc;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Kyc;

public sealed class GetMyVerificationStatusQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<GetMyVerificationStatusQueryHandler> logger)
    : IRequestHandler<GetMyVerificationStatusQuery, Result<VerificationStatusResponse>>
{
    public async Task<Result<VerificationStatusResponse>> Handle(
        GetMyVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Result<VerificationStatusResponse>.Fail("Authentication required");
        }

        var all = await unitOfWork.Repository<KycVerification>()
            .FindAsync(x => x.UserId == userId, cancellationToken);
        if (all.Count == 0)
        {
            return Result<VerificationStatusResponse>.Ok(
                new VerificationStatusResponse(null, string.Empty, "none", null));
        }

        // The open record is what the frontend is waiting on (at most one
        // exists); otherwise show the most recently decided one.
        var current = all.FirstOrDefault(x => x.Status == KycVerificationStatus.Pending)
            ?? all.OrderByDescending(x => x.Outcome?.CompletedAt ?? DateTimeOffset.MinValue).First();

        var status = current.Status switch
        {
            KycVerificationStatus.Verified => "verified",
            KycVerificationStatus.Rejected => "rejected",
            _ => "pending",
        };

        logger.LogDebug("KYC status for user {UserId}: {Status}", userId, status);
        return Result<VerificationStatusResponse>.Ok(new VerificationStatusResponse(
            current.Id, current.Outcome?.Provider ?? string.Empty, status, current.Outcome?.CompletedAt));
    }
}
