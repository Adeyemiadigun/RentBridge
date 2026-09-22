using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.PayoutAccount;

/// <summary>
/// Name enquiry against the payment provider — returns the account holder's
/// name for a bank + account number so the user can confirm before saving.
/// Nothing is persisted.
/// </summary>
public sealed record ResolvePayoutAccountCommand(string BankCode, string AccountNumber) : IRequest<Result<string>>;
