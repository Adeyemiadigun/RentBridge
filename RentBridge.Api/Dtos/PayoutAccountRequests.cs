namespace RentBridge.Api.Dtos;

public sealed record ResolvePayoutAccountRequest(string BankCode, string AccountNumber);

public sealed record SetPayoutAccountRequest(string BankCode, string BankName, string AccountNumber);
