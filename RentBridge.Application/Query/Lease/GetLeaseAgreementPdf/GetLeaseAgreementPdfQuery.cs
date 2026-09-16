using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Lease;

/// <summary>
/// Renders the stored agreement document (terms + certification + signature
/// evidence) as an immutable PDF artifact. Accessible to the landlord, tenant,
/// assigned lawyer, or an admin.
/// </summary>
public sealed record GetLeaseAgreementPdfQuery(Guid LeaseId) : IRequest<Result<byte[]>>;