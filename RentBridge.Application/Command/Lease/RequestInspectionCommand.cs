using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class RequestInspectionCommand(Guid LeaseId, DateTimeOffset PreferredDate, string? Note = null)
    : IRequest<Result>;