namespace RentBridge.Api.Dtos;

/// <summary>
/// Records that the inspection physically took place. This is what satisfies
/// the escrow release gate, so the actual date cannot be in the future.
/// </summary>
public sealed record CompleteInspectionRequest(DateTimeOffset ActualDate, string? Notes = null);
