using MediatR;
using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Transaction;

/// <summary>
/// Returns the escrow ledger lines for one lease, newest first. Accessible to
/// the lease's landlord and tenant, its assigned lawyer, or an admin.
/// </summary>
public sealed record GetLeaseTransactionsQuery(Guid LeaseId, int Page = 1, int PageSize = 20)
    : IRequest<Result<TransactionHistoryResponse>>;
