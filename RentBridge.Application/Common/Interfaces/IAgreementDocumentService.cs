using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Composes (once, idempotently) the canonical agreement content for a lease
/// from its listings/users data, hashes it, and back-fills it onto the lease.
/// The resulting snapshot is what the lawyer certifies and parties sign.
/// </summary>
public interface IAgreementDocumentService
{
    Task<Result<AgreementDocument>> EnsureComposedAsync(Lease lease, CancellationToken cancellationToken);
}