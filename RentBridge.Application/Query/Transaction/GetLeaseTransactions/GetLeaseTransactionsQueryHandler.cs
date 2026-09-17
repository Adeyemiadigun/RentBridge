using MediatR;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Dtos.Transaction;
using RentBridge.Domain.Common;
using RentBridge.Domain.Entities;
using RentBridge.Domain.Enums;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;

namespace RentBridge.Application.Query.Transaction;

public sealed class GetLeaseTransactionsQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<GetLeaseTransactionsQuery, Result<TransactionHistoryResponse>>
{
    public async Task<Result<TransactionHistoryResponse>> Handle(
        GetLeaseTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var current = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!current.IsSuccess)
        {
            return Result<TransactionHistoryResponse>.Fail(current.Error!);
        }
        var user = current.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            return Result<TransactionHistoryResponse>.Fail("Lease not found");
        }

        var isParty = user.Id == lease.LandlordUserId || user.Id == lease.TenantUserId;
        var isAssignedLawyer = user.Id == lease.AssignedLawyerId;
        var isAdmin = user.Role is UserRole.Admin;
        if (!isParty && !isAssignedLawyer && !isAdmin)
        {
            return Result<TransactionHistoryResponse>.Fail("You do not have access to this lease.");
        }

        var page = await unitOfWork.Repository<LedgerEntry>().GetPagedAsync(
            e => e.LeaseId == request.LeaseId,
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
