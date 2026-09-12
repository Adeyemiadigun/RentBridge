using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Property;

public sealed record RejectDocumentCommand(Guid PropertyId, Guid DocumentId, string? Reason = null) : IRequest<Result>;