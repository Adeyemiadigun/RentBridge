using MediatR;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Property
{
    public record class ListPropertyCommand(Guid PropertyId,
    string Title,
    decimal PriceAmount,
    string? Description = null) : IRequest<Result<Guid>>;
}
