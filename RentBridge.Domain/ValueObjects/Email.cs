using RentBridge.Domain.Common;
using System.Text.RegularExpressions;

namespace RentBridge.Domain.ValueObjects
{
    public sealed record Email
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Result<Email> Create(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result<Email>.Fail("Email cannot be empty.");
            }

            string trimmedEmail = email.Trim();

            if (trimmedEmail.Length > 254)
            {
                return Result<Email>.Fail("Email is too long.");
            }

            if (!EmailRegex.IsMatch(trimmedEmail))
            {
                return Result<Email>.Fail("Invalid email format.");
            }

            return Result<Email>.Ok(new Email(trimmedEmail));
        }

        public static implicit operator string(Email email) => email.Value;
    }
}
