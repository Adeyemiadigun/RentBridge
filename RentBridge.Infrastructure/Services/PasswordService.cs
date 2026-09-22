using RentBridge.Application.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace RentBridge.Infrastructure.Services
{
    internal class PasswordService : IPasswordService
    {
        public (string,string) Generate(string password)
        {
            
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return (passwordHash, salt);
        }

        public bool VerifyPassword(string password, string passwordHash, string salt)
        {
            
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
