using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

/// <summary>
/// Records that the inspection physically took place, stamping the second of
/// the three escrow release gates. Landlord or admin only.
/// </summary>
public sealed record CompleteInspectionCommand(
    Guid LeaseId,
    DateTimeOffset ActualDate,
    string? Notes = null) : IRequest<Result<LeaseTransitionResponse>>;
