using MediatR;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Lease;

/// <summary>
/// Returns full lease detail including agreement certification/signature status
/// and the escrow payment trail. Accessible to the landlord, tenant,
/// assigned lawyer, or an admin.
/// </summary>
public sealed record GetLeaseQuery(Guid LeaseId) : IRequest<Result<LeaseDetailResponse>>;