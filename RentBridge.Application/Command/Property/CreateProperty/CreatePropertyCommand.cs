using MediatR;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Property
{
    

    public sealed record CreatePropertyCommand(string Street,
        string City,
        string Area,
        string State,
        List<string> DocumentUrls) : IRequest<Result<Guid>>;
}
