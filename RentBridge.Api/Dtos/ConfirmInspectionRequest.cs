namespace RentBridge.Api.Dtos;

public sealed record ConfirmInspectionRequest(DateTimeOffset? ScheduledDate = null, string? Notes = null);