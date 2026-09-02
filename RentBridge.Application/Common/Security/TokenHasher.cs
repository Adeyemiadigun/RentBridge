using System.Security.Cryptography;
using System.Text;

namespace RentBridge.Application.Common.Security
{
    public static class TokenHasher
    {
        public static string Hash(string rawToken)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(bytes);
        }
    }
}
