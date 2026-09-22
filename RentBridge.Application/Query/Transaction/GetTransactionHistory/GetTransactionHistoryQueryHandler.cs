using System.Linq.Expressions;
using MediatR;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Query.Transaction;

public sealed class GetTransactionHistoryQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<GetTransactionHistoryQuery, Result<TransactionHistoryResponse>>
{
    public async Task<Result<TransactionHistoryResponse>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var current = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!current.IsSuccess)
        {
            return Result<TransactionHistoryResponse>.Fail(current.Error!);
        }
        var user = current.Value;

        Expression<Func<LedgerEntry, bool>>? predicate = user.Role is UserRole.Admin
            ? null
            : e => e.TenantUserId == user.Id || e.LandlordUserId == user.Id;

        var page = await unitOfWork.Repository<LedgerEntry>().GetPagedAsync(
            predicate,
            request.Page,
            request.PageSize,
            orderBy: e => e.OccurredAt,
            ascending: false,
            ct: cancellationToken);

        var items = page.Items
            .Select(e => new TransactionItem(
                e.Id,
                e.LeaseId,
                e.EscrowPaymentId,
                e.Type,
                e.Direction,
                e.Amount,
                e.Currency,
                e.Reference,
                e.Status,
                e.Attempt,
                e.Description,
                e.OccurredAt))
            .ToList();

        return Result<TransactionHistoryResponse>.Ok(
            new TransactionHistoryResponse(items, page.Page, page.PageSize, page.TotalCount, page.TotalPages));
    }
}
