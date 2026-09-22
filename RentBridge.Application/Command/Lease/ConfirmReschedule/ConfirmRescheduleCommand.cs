using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class ConfirmRescheduleCommand(Guid LeaseId) : IRequest<Result>;