using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.ApplyVerificationResult;

/// <summary>
/// Applies a vendor callback result (Smile ID) to a pending verification.
/// A single Smile job returns one complete outcome (ID check + selfie),
/// so one apply decides: pass raises IdentityVerified, otherwise Rejected.
/// </summary>
public sealed record ApplyVerificationResultCommand(
    Guid KycVerificationId,
    bool Passed,
    string Provider,    // e.g. "smile"
    string ProviderRef, // vendor job/callback reference id
    DateTimeOffset? CompletedAt = null
) : IRequest<Result>;