using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Interfaces.Verification;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed class SubmitIdentityVerificationCommandHandler(
    ILogger<SubmitIdentityVerificationCommandHandler> logger,
    IUnitOfWork _unitOfWork,
    ICurrentUser currentUser,
    IIdentityVerificationProviderFactory providers)
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

            IIdentityVerificationProvider provider;
            try
            {
                provider = string.IsNullOrWhiteSpace(request.Provider)
                    ? providers.GetDefault()
                    : providers.Get(request.Provider);
            }
            catch (InvalidOperationException ex)
            {
                return Result<KycSubmissionResponse>.Fail(ex.Message);
            }

            if (provider.SupportsSdkSession is false)
            {
                return Result<KycSubmissionResponse>.Fail(
                    $"{provider.ProviderName} does not support the app SDK flow.");
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

            // Build the frontend widget bootstrap. Biometrics are captured
            // on-device by the vendor SDK; the verdict arrives via webhook.
            var session = await provider.CreateSdkSessionAsync(kyc.Id, nin.Value.Value, cancellationToken);
            if (session.IsSuccess is false)
            {
                return Result<KycSubmissionResponse>.Fail(session.Error);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("KYC started: {KycId} for user {UserId} via {Provider}",
                kyc.Id, user.Id, provider.ProviderName);

            return Result<KycSubmissionResponse>.Ok(new KycSubmissionResponse(
                kyc.Id, provider.ProviderName, null, null, null, session.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error starting identity verification for user {UserId}", currentUser.UserId);
            return Result<KycSubmissionResponse>.Fail("An unexpected error occurred while starting identity verification");
        }
    }
}
