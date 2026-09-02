using MediatR;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Auth
{
    public record LoginCommand(): IRequest<Result<LoginResponse>>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
