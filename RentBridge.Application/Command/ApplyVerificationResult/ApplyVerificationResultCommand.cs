using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.ApplyVerificationResult;

/// <summary>
/// Applies a KYC vendor callback result (NIN or facial) to a pending
/// verification. If both halves now pass, the aggregate raises IdentityVerified
/// and the user badge flips.
/// </summary>
public sealed record ApplyVerificationResultCommand(
    Guid KycVerificationId,
    string Kind,        // "identity" | "facial"
    bool Passed,
    string Provider,    // e.g. "dojah"
    string ProviderRef, // vendor event/reference id
    DateTimeOffset? CompletedAt = null
) : IRequest<Result>;
