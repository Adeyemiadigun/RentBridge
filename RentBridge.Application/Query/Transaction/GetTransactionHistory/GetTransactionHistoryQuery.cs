using MediatR;
using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Transaction;

/// <summary>
/// Returns the caller's escrow ledger lines, newest first. A tenant sees lines
/// for leases they rent, an owner sees lines for leases they own, and an admin
/// sees every line.
/// </summary>
public sealed record GetTransactionHistoryQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<TransactionHistoryResponse>>;
