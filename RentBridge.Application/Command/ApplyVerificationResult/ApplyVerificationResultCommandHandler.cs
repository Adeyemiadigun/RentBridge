using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.ApplyVerificationResult;

public sealed class ApplyVerificationResultCommandHandler(
    ILogger<ApplyVerificationResultCommandHandler> logger,
    IUnitOfWork _unitOfWork)
    : IRequestHandler<ApplyVerificationResultCommand, Result>
{
    public async Task<Result> Handle(ApplyVerificationResultCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var kyc = await _unitOfWork.Repository<KycVerification>().GetByIdAsync(request.KycVerificationId, cancellationToken);
            if (kyc is null)
            {
                return Result.Fail("Verification record not found");
            }

            var result = VerificationResult.Create(request.Provider, request.ProviderRef, request.Passed, request.CompletedAt);
            if (result.IsSuccess is false)
            {
                return Result.Fail(result.Error);
            }

            Result apply = request.Kind.ToLowerInvariant() switch
            {
                "identity" => kyc.ApplyIdentityResult(result.Value),
                "facial" => kyc.ApplyFacialResult(result.Value),
                _ => Result.Fail($"Unknown verification kind: {request.Kind}")
            };

            if (apply.IsSuccess is false)
            {
                return apply;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Applied {Kind} verification result to {KycId}: passed={Passed}",
                request.Kind, request.KycVerificationId, request.Passed);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error applying verification result for {KycId}", request.KycVerificationId);
            return Result.Fail("An unexpected error occurred while applying the verification result");
        }
    }
}
