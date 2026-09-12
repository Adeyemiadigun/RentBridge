using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class CancelInspectionCommand(Guid LeaseId) : IRequest<Result>;