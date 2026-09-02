using RentBridge.Domain.Common;
using System.Text.RegularExpressions;

namespace RentBridge.Domain.ValueObjects
{
    public sealed record PhoneNumber
    {
        private static readonly Regex E164Regex = new(
            @"^\+[1-9]\d{1,14}$",
            RegexOptions.Compiled
        );

        public string Value { get; }

        private PhoneNumber(string value)
        {
            Value = value;
        }

        public static Result<PhoneNumber> Create(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Result<PhoneNumber>.Fail("Phone number cannot be empty.");
            }

            string cleaned = Regex.Replace(input.Trim(), @"[^\d+]", "");

            if (cleaned.StartsWith("0") && cleaned.Length == 11)
            {
                cleaned = "+234" + cleaned.Substring(1);
            }
            else if (cleaned.StartsWith("234") && cleaned.Length == 13)
            {
                cleaned = "+" + cleaned;
            }

            if (!E164Regex.IsMatch(cleaned))
            {
                return Result<PhoneNumber>.Fail("Invalid phone number format. Must be a valid E.164 number (e.g., +234...).");
            }

            return Result<PhoneNumber>.Ok(new PhoneNumber(cleaned));
        }

        public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    }
}
