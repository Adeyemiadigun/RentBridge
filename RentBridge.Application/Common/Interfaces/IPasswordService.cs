using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Common.Interfaces
{
    public interface IPasswordService
    {
        (string, string) Generate(string password);
        bool VerifyPassword(string password, string passwordHash, string salt);
    }
}
