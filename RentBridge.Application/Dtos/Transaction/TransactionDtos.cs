using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Transaction;

/// <summary>A single ledger line as shown in a user's transaction history.</summary>
public sealed record TransactionItem(
    Guid Id,
    Guid LeaseId,
    Guid EscrowPaymentId,
    TransactionType Type,
    TransactionDirection Direction,
    decimal Amount,
    string Currency,
    string? Reference,
    EscrowStatus Status,
    int Attempt,
    string Description,
    DateTimeOffset OccurredAt);

/// <summary>One page of a user's transaction history, newest first.</summary>
public sealed record TransactionHistoryResponse(
    IReadOnlyList<TransactionItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
