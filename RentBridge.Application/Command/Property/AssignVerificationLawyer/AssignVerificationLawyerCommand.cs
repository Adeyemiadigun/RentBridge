using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public record AssignVerificationLawyerCommand(Guid PropertyId, Guid LawyerId) : IRequest<Result>;