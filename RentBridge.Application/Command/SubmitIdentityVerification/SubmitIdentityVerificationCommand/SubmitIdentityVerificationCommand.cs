using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

/// <summary>
/// Starts an identity verification with the active provider (default Dojah).
/// The backend validates the NIN, creates the pending KYC record, and returns
/// the SDK/widget bootstrap. The frontend opens the vendor widget, which
/// captures biometrics on-device; the verdict arrives via webhook.
/// No images travel through our API.
/// </summary>
public sealed record SubmitIdentityVerificationCommand(
    string Nin,
    string? Provider = null
) : IRequest<Result<KycSubmissionResponse>>;
