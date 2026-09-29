using MediatR;
using RentBridge.Application.Dtos.Kyc;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Kyc;

/// <summary>
/// Returns the current user's latest KYC state for polling after the
/// vendor widget completes. Prefers the open Pending record; otherwise
/// returns the most recently decided one.
/// </summary>
public sealed record GetMyVerificationStatusQuery() : IRequest<Result<VerificationStatusResponse>>;
