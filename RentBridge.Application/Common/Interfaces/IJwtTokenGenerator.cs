using RentBridge.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        Task<string> GenerateTokenAsync(User user, CancellationToken ct);
        string GenerateRefreshToken();
    }
}
