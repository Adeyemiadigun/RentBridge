namespace RentBridge.Domain.Enums;

/// <summary>
/// How a ledger line affects the account it is shown against.
/// </summary>
public enum TransactionDirection
{
    /// <summary>Money into the user's account (e.g. rent received, payout received).</summary>
    Credit,

    /// <summary>Money out of the user's account (e.g. rent paid, fee deducted).</summary>
    Debit,

    /// <summary>Not a movement — a status event (e.g. a failed payout attempt).</summary>
    Info,
}
