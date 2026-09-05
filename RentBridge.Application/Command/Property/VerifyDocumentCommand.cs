using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public sealed record VerifyDocumentCommand(Guid PropertyId, Guid DocumentId) : IRequest<Result>;