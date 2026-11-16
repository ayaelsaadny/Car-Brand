using System.Text.RegularExpressions;

namespace Limousine.Application.Common
{
    public static class PhoneHelper
    {
        /// <summary>
        /// Normalizes a North American phone number into the standard format: +1 XXX XXX XXXX
        /// </summary>
        public static string NormalizeNorthAmericanPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var trimmed = phone.Trim();
            // Extract all digits
            var digitsOnly = Regex.Replace(trimmed, @"\D", "");

            // If 11 digits starting with 1 (e.g. 16479525412)
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                var area = digitsOnly.Substring(1, 3);
                var prefix = digitsOnly.Substring(4, 3);
                var line = digitsOnly.Substring(7, 4);
                return $"+1 {area} {prefix} {line}";
            }

            // If 10 digits (e.g. 6479525412)
            if (digitsOnly.Length == 10)
            {
                var area = digitsOnly.Substring(0, 3);
                var prefix = digitsOnly.Substring(3, 3);
                var line = digitsOnly.Substring(6, 4);
                return $"+1 {area} {prefix} {line}";
            }

            return trimmed;
        }
    }
}
