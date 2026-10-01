namespace RentBridge.Api.Dtos;

/// <summary>
/// Accepts the inspection and books it. The scheduled date is REQUIRED —
/// a confirmation without a date is not a real booking.
/// </summary>
public sealed record ConfirmInspectionRequest(DateTimeOffset ScheduledDate, string? Notes = null);