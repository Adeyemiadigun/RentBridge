using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class RejectRescheduleCommand(Guid LeaseId) : IRequest<Result>;