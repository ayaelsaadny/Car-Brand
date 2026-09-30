using Limousine.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerProfileDto
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalBookings { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }

        public string GetEffectiveFullName()
        {
            if (!string.IsNullOrWhiteSpace(FullName))
                return FullName.Trim();

            var combined = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrWhiteSpace(combined) ? "Customer" : combined;
        }

        public (string FirstName, string LastName) GetNames()
        {
            if (!string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName))
            {
                return (FirstName?.Trim() ?? string.Empty, LastName?.Trim() ?? string.Empty);
            }

            var parts = (FullName ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return (parts.Length > 0 ? parts[0] : "Customer", parts.Length > 1 ? parts[1] : string.Empty);
        }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm new password is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ToggleUserStatusDto
    {
        public bool IsActive { get; set; }
    }
}
