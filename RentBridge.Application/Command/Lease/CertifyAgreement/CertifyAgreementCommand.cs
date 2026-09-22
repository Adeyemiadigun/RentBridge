using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Marks the lease agreement as certified by the assigned lawyer (LegalReview → Certified).
/// </summary>
public sealed record CertifyAgreementCommand(Guid LeaseId) : IRequest<Result>;