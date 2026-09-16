namespace RentBridge.Application.Dtos.Admin;

public sealed record FeeSettingsResponse(decimal PlatformCommissionRate, decimal LegalFeeRate, DateTimeOffset UpdatedAt);