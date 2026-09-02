using MediatR;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.RegisterUser
{

    public sealed record registerUserCommand(
        string Email,
        string Phone,
        string FirstName,
        string LastName,
        string Role,
        string Password,
        string? BarNumber = null
    ) : IRequest<Result<Guid>>;
   

}
