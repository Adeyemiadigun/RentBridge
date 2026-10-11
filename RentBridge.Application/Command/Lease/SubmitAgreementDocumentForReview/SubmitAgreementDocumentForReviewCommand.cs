using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// A party sends their uploaded signed agreement to the assigned lawyer for
/// review (LandlordSigning → LandlordDocInReview / TenantSigning →
/// TenantDocInReview).
/// </summary>
public sealed record SubmitAgreementDocumentForReviewCommand(Guid LeaseId) : IRequest<Result>;
