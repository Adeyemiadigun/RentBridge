using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Dtos.Auth
{
    public record LoginResponse
    {
        public string Token { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
