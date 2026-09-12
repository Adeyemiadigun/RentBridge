using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Lease;

public record class CreateLeaseCommand(Guid ListingId) : IRequest<Result<Guid>>;