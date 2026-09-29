using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public record SubmitPropertyForReviewCommand(Guid PropertyId) : IRequest<Result>;