using RentBridge.Domain.Common;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Owned 1:1 child of User: the verified bank destination for escrow payouts.
/// Only ever created from a provider-verified name enquiry + transfer recipient,
/// so the stored account name is authoritative rather than user-supplied.
/// </summary>
public class PayoutAccount
{
    public string Provider { get; private set; } = string.Empty;
    public string RecipientCode { get; private set; } = string.Empty;
    public string BankCode { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string AccountNumberLast4 { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset VerifiedAt { get; private set; }

    private PayoutAccount() { }

    public static Result<PayoutAccount> Create(
        string provider,
        string recipientCode,
        string bankCode,
        string bankName,
        string accountNumberLast4,
        string accountName,
        DateTimeOffset verifiedAt)
    {
        if (string.IsNullOrWhiteSpace(provider)) return Result<PayoutAccount>.Fail("Payout provider is required.");
        if (string.IsNullOrWhiteSpace(recipientCode)) return Result<PayoutAccount>.Fail("Recipient code is required.");
        if (string.IsNullOrWhiteSpace(bankCode)) return Result<PayoutAccount>.Fail("Bank code is required.");
        if (string.IsNullOrWhiteSpace(accountName)) return Result<PayoutAccount>.Fail("Account name is required.");
        if (accountNumberLast4 is not { Length: 4 }) return Result<PayoutAccount>.Fail("Account number must have 4 digits.");

        return Result<PayoutAccount>.Ok(new PayoutAccount
        {
            Provider = provider.Trim(),
            RecipientCode = recipientCode.Trim(),
            BankCode = bankCode.Trim(),
            BankName = (bankName ?? string.Empty).Trim(),
            AccountNumberLast4 = accountNumberLast4,
            AccountName = accountName.Trim(),
            IsActive = true,
            VerifiedAt = verifiedAt,
        });
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Fail("Payout account is already inactive.");
        IsActive = false;
        return Result.Ok();
    }
}
