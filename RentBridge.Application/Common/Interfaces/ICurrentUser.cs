using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Common.Interfaces
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }
        string? Email { get; }
    }
}
