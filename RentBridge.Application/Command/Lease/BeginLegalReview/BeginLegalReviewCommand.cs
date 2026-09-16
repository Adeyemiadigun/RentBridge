using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Moves a lease into legal review (InspectionConfirmed → LegalReview).
/// If no lawyer is assigned yet (auto-assignment found no verified lawyer at
/// inspection-confirm time), one is picked lazily before the transition.
/// </summary>
public sealed record BeginLegalReviewCommand(Guid LeaseId) : IRequest<Result>;