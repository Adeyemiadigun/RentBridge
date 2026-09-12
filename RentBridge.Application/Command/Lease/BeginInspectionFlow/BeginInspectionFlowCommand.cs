using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class BeginInspectionFlowCommand(Guid LeaseId) : IRequest<Result>;