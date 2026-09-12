using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class ConfirmInspectionCommand(Guid LeaseId, DateTimeOffset? ScheduledDate = null, string? Notes = null) : IRequest<Result>;