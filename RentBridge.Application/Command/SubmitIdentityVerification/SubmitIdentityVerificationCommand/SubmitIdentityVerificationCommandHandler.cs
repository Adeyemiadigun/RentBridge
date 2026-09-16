using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed class SubmitIdentityVerificationCommandHandler(
    ILogger<SubmitIdentityVerificationCommandHandler> logger,
    IUnitOfWork _unitOfWork,
    ICurrentUser currentUser,
    IIdentityVerificationService verificationService)
    : IRequestHandler<SubmitIdentityVerificationCommand, Result<KycSubmissionResponse>>
{
    public async Task<Result<KycSubmissionResponse>> Handle(
        SubmitIdentityVerificationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is not Guid userId)
            {
                return Result<KycSubmissionResponse>.Fail("Authentication required");
            }

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result<KycSubmissionResponse>.Fail("User not found");
            }

            var nin = NinNumber.Create(request.Nin);
            if (nin.IsSuccess is false)
            {
                return Result<KycSubmissionResponse>.Fail(nin.Error);
            }

            // One active verification per user at a time.
            var hasActive = await _unitOfWork.Repository<KycVerification>()
                .AnyAsync(x => x.UserId == userId && x.Status == KycVerificationStatus.Pending, cancellationToken);
            if (hasActive)
            {
                return Result<KycSubmissionResponse>.Fail("A verification is already pending for this user");
            }

            var kyc = new KycVerification(user.Id, nin.Value);
            _unitOfWork.Repository<KycVerification>().Add(kyc);

            // Mint the JWT the client SDK uses to submit the biometric_kyc job.
            // The verdict arrives later via the webhook (partner_params.kyc_id).
            var token = await verificationService.GetTokenAsync(cancellationToken);
            if (token.IsSuccess is false)
            {
                return Result<KycSubmissionResponse>.Fail(token.Error);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("KYC started: {KycId} for user {UserId}", kyc.Id, user.Id);

            return Result<KycSubmissionResponse>.Ok(
                new KycSubmissionResponse(kyc.Id, token.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error starting identity verification for user {UserId}", currentUser.UserId);
            return Result<KycSubmissionResponse>.Fail("An unexpected error occurred while starting identity verification");
        }
    }
}