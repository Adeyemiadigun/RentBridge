using RentBridge.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RentBridge.Infrastructure.Services
{
    internal class PasswordService : IPasswordService
    {
        public (string,string) Generate(string password)
        {
            var salt = Guid.NewGuid().ToString("N").Substring(0, 8);
            var hashSalt = ComputeSHA256(salt);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, hashSalt);
            return (passwordHash, hashSalt);
        }

        public bool VerifyPassword(string password, string passwordHash, string salt)
        {
            var hashToVerify = BCrypt.Net.BCrypt.HashPassword(password, salt);

            return BCrypt.Net.BCrypt.Verify(hashToVerify, passwordHash);
        }

        static string ComputeSHA256(string rawData)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }
    }
}
