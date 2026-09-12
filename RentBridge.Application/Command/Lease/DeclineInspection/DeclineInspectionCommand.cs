using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class DeclineInspectionCommand(Guid LeaseId) : IRequest<Result>;