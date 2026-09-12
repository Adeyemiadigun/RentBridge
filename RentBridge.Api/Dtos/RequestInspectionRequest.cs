namespace RentBridge.Api.Dtos;

public sealed record RequestInspectionRequest(DateTimeOffset PreferredDate, string? Note = null);