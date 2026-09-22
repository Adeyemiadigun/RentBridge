using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.PayoutAccount;

/// <summary>
/// Landlord/agent/caretaker registers the bank account that receives escrow
/// payouts. The account name is verified by the provider (name enquiry), a
/// transfer recipient is created, and the verified account is stored on the user.
/// Idempotent — re-registering replaces the previous account.
/// </summary>
public sealed record SetPayoutAccountCommand(
    string BankCode,
    string BankName,
    string AccountNumber) : IRequest<Result>;
