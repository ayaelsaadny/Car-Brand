using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Limousine.Application.Validation
{
    /// <summary>
    /// Validates that a phone number adheres to the North American Numbering Plan (US & Canada, +1).
    /// Format: Country Code (+1), 3-digit Area Code, and 7-digit local subscriber number.
    /// Example: +1 647 952 5412, +1 (647) 952-5412, +1-647-952-5412, +16479525412, or 10-digit North American numbers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NorthAmericanPhoneAttribute : ValidationAttribute
    {
        // Pattern matches:
        // +1 647 952 5412, +1 647  952 5412, +1(647)952-5412, +1-647-952-5412, +16479525412, 16479525412
        // as well as standard 10 digits: (647) 952-5412, 647 952 5412, 647-952-5412
        // NANP Area codes start with digits 2-9.
        public const string Pattern = @"^(\+?1\s*[-.]?\s*)?\(?[2-9][0-9]{2}\)?\s*[-.]?\s*[0-9]{3}\s*[-.]?\s*[0-9]{4}$";

        public static readonly Regex PhoneRegex = new(Pattern, RegexOptions.Compiled);

        public NorthAmericanPhoneAttribute()
            : base("Phone number must be a valid US/Canada number (e.g. +1 647 952 5412).")
        {
        }

        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            return PhoneRegex.IsMatch(phone.Trim());
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
                return ValidationResult.Success; // Emptiness is validated by [Required] if needed

            if (IsValidPhone(str))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? "Phone number must be a valid US/Canada number (e.g. +1 647 952 5412).");
        }
    }
}
