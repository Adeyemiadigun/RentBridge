using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Common.Interfaces
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }
        string? Email { get; }
        Task<Result<User>> GetCurrentUser(bool isIdentityVerified = false, CancellationToken cancellationToken = default);
    }
}
