using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RentBridge.Domain.ValueObjects
{
    public sealed class NinNumber
    {
        public string Value { get; }

        private NinNumber(string value)
        {
            Value = value;
        }

        public static Result<NinNumber> Create(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Result<NinNumber>.Fail("NIN number cannot be empty.");
            }
            string cleaned = Regex.Replace(input.Trim(), @"[^\d]", "");
            if (cleaned.Length != 11 || !long.TryParse(cleaned, out _))
            {
                return Result<NinNumber>.Fail("Invalid NIN number format. Must be 11 digits.");
            }
            return Result<NinNumber>.Ok(new NinNumber(cleaned));
        }
    }
}
