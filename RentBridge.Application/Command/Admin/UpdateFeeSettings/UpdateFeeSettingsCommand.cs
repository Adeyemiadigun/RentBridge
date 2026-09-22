using MediatR;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Admin-only: sets the global escrow fee-split percentages. Applied to all
/// future escrow payments (existing payments keep their recorded split).
/// </summary>
public sealed record UpdateFeeSettingsCommand(decimal PlatformCommissionRate, decimal LegalFeeRate)
    : IRequest<Result<FeeSettingsResponse>>;