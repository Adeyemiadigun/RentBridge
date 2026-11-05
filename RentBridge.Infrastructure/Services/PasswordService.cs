using RentBridge.Application.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace RentBridge.Infrastructure.Services
{
    internal class PasswordService : IPasswordService
    {
        // BCrypt embeds its salt inside the hash string itself: the 29-char salt
        // string returned by GenerateSalt() is exactly the first 29 chars of the
        // resulting hash. We store that salt separately so VerifyPassword
        // can bind hash and salt together before comparing passwords (fails
        // closed on rows where the pair disagrees, e.g. hand-edited data).
        public (string, string) Generate(string password)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return (passwordHash, salt);
        }

        public bool VerifyPassword(string password, string passwordHash, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            // Fail closed if the stored salt is not the salt the hash was built
            // with — a mismatch means the row is inconsistent.
            return passwordHash.StartsWith(salt, StringComparison.Ordinal)
                && BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
