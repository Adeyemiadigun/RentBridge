using MediatR;
using Microsoft.AspNetCore.Http;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed record SubmitIdentityVerificationCommand(
    Guid UserId,
    string Nin,
    IFormFile facialImage,
    IFormFile[]? LivenessImages = null
) : IRequest<Result<Guid>>;