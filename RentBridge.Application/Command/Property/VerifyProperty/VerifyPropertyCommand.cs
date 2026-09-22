using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public sealed record VerifyPropertyCommand(Guid PropertyId) : IRequest<Result>;