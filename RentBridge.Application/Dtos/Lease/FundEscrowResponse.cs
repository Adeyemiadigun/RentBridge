namespace RentBridge.Application.Dtos.Lease;

public sealed record FundEscrowResponse(string CheckoutUrl, string Reference, string Status);