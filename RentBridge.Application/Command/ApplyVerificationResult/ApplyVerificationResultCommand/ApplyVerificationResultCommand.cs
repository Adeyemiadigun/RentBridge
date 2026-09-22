using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.ApplyVerificationResult;

/// <summary>
/// Applies a vendor callback result (Smile ID, Dojah widget) to a pending verification.
/// Webhook vendors return one complete outcome (ID check + selfie) per session,
/// so one apply decides: pass raises IdentityVerified, otherwise Rejected.
/// </summary>
public sealed record ApplyVerificationResultCommand(
    Guid KycVerificationId,
    bool Passed,
    string Provider,    // e.g. "dojah", "smile"
    string ProviderRef, // vendor job/callback reference id
    DateTimeOffset? CompletedAt = null
) : IRequest<Result>;