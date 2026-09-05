using MediatR;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Property
{
    public sealed record VerifyDocCommand(Guid DocumentId) : IRequest<Result<Guid>>;
}
