using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public record DeletePropertyCommand(Guid PropertyId) : IRequest<Result>;