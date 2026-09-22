using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

/// <summary>
/// Starts an identity verification with the active provider (default Dojah).
/// Sync providers (Dojah) verify NIN+selfie immediately; SDK providers
/// (Smile) mint a client session and the verdict arrives via webhook.
/// </summary>
public sealed record SubmitIdentityVerificationCommand(
    string Nin,
    string? SelfieImage = null,
    string? FirstName = null,
    string? LastName = null,
    string? Provider = null
) : IRequest<Result<KycSubmissionResponse>>;
