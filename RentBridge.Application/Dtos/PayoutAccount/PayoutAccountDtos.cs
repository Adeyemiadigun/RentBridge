namespace RentBridge.Application.Dtos.PayoutAccount;

public sealed record BankDto(string Code, string Name, string? Slug);

public sealed record PayoutAccountDto(
    string Provider,
    string BankName,
    string BankCode,
    string AccountNumberLast4,
    string AccountName,
    bool IsActive,
    DateTimeOffset VerifiedAt);
