using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class RequestRescheduleCommand(Guid LeaseId, DateTimeOffset NewDate, string? Note = null) : IRequest<Result>;