using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Adds a signed signature for the requesting party (tenant or landlord).
/// <paramref name="SignatureImage"/> is an optional base64-encoded image of the
/// drawn signature; when omitted a deterministic hash is produced instead.
/// </summary>
public sealed record SignAgreementCommand(Guid LeaseId, string? SignatureImage = null) : IRequest<Result>;