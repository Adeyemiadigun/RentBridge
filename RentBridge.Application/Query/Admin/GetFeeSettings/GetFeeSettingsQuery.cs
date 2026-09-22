using MediatR;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Admin;

/// <summary>
/// Admin-only: current global escrow fee-split percentages.
/// </summary>
public sealed record GetFeeSettingsQuery : IRequest<Result<FeeSettingsResponse>>;