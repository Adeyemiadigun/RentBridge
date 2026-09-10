using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed record SubmitIdentityVerificationCommand(
    string Nin
) : IRequest<Result<KycSubmissionResponse>>;