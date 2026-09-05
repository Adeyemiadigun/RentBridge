using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed class SubmitIdentityVerificationCommandHandler(
    ILogger<SubmitIdentityVerificationCommandHandler> logger,
    IUnitOfWork _unitOfWork,
    IIdentityVerificationService verificationService)
    : IRequestHandler<SubmitIdentityVerificationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitIdentityVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                return Result<Guid>.Fail("User not found");
            }

            var nin = NinNumber.Create(request.Nin);
            if (nin.IsSuccess is false)
            {
                return Result<Guid>.Fail(nin.Error);
            }

            // One active verification per user at a time.
            var hasActive = await _unitOfWork.Repository<KycVerification>()
                .AnyAsync(x => x.UserId == request.UserId && x.Status == KycVerificationStatus.Pending, cancellationToken);
            if (hasActive)
            {
                return Result<Guid>.Fail("A verification is already pending for this user");
            }

            var kyc = new KycVerification(user.Id, nin.Value);
            _unitOfWork.Repository<KycVerification>().Add(kyc);

            // Kick off the Smile ID biometric_kyc job (NIN + selfie). The outcome
            // arrives asynchronously via webhook and is applied to this kyc record.
            var subject = new VerificationSubject(
                GivenNames: user.FirstName,
                LastName: user.LastName,
                Email: user.Email.Value,
                PhoneNumber: user.Phone.Value);

            var submitted = await verificationService.SubmitVerificationAsync(
                kyc.Id,
                nin.Value,
                subject,
                request.facialImage,
                request.LivenessImages ?? Array.Empty<IFormFile>(),
                cancellationToken);

            if (submitted.IsSuccess is false)
            {
                return Result<Guid>.Fail(submitted.Error);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Identity verification submitted: {KycId} for user {UserId} (job {JobId})",
                kyc.Id, user.Id, submitted.Value);

            return Result<Guid>.Ok(kyc.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error submitting identity verification for user {UserId}", request.UserId);
            return Result<Guid>.Fail("An unexpected error occurred while submitting identity verification");
        }
    }
}