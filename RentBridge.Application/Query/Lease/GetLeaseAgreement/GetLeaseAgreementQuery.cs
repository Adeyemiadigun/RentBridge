using MediatR;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Lease;

/// <summary>
/// Returns the canonical tenancy agreement content (terms + pinned hash + current
/// certification/signature status) for a lease. Composes the draft lazily if it
/// does not exist yet. Accessible to the landlord, tenant, assigned lawyer, or an admin.
/// </summary>
public sealed record GetLeaseAgreementQuery(Guid LeaseId) : IRequest<Result<LeaseAgreementResponse>>;