using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Enums;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            var email = dto.Email.Trim().ToLower();
            var user = await _unitOfWork.Repository<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = _jwtTokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<AuthResponseDto?> AdminLoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            var email = dto.Email.Trim().ToLower();
            var user = await _unitOfWork.Repository<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                return null;
            }

            // Enforce admin privilege check explicitly
            if (user.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Access denied. The specified account does not possess administrator privileges.");
            }

            var token = _jwtTokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
        {
            // 1. Password Confirmation Check (default to Password if omitted)
            if (string.IsNullOrEmpty(dto.ConfirmPassword))
            {
                dto.ConfirmPassword = dto.Password;
            }

            if (dto.Password != dto.ConfirmPassword)
            {
                throw new InvalidOperationException("The password and confirmation password do not match.");
            }

            // 2. Enforce Configured Password Policy
            ValidatePasswordPolicy(dto.Password);

            // 3. Email Uniqueness Rule
            var email = dto.Email.Trim().ToLower();
            var exists = await _unitOfWork.Repository<ApplicationUser>()
                .AnyAsync(u => u.Email == email, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"A customer account with the email address '{dto.Email}' already exists.");
            }

            // Public customer registration always assigns Customer role
            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ApplicationUser>().AddAsync(user, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var names = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var customer = new Customer
            {
                UserId = user.Id,
                FirstName = names.Length > 0 ? names[0] : user.FullName,
                LastName = names.Length > 1 ? names[1] : string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var token = _jwtTokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<UserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(userId, cancellationToken);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<CustomerProfileDto?> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(userId, cancellationToken);
            if (user == null) return null;

            var customer = await _unitOfWork.Repository<Customer>()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            var bookingCount = 0;
            if (customer != null)
            {
                bookingCount = await _unitOfWork.Repository<BookingRequest>()
                    .CountAsync(b => b.CustomerId == customer.Id, cancellationToken);
            }

            var names = (user.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            return new CustomerProfileDto
            {
                UserId = user.Id,
                CustomerId = customer?.Id ?? 0,
                FirstName = customer?.FirstName ?? (names.Length > 0 ? names[0] : (user.FullName ?? string.Empty)),
                LastName = customer?.LastName ?? (names.Length > 1 ? names[1] : string.Empty),
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = customer?.PhoneNumber ?? user.PhoneNumber ?? string.Empty,
                Address = customer?.Address,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<CustomerProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(userId, cancellationToken);
            if (user == null) return null;

            var fullName = dto.GetEffectiveFullName();
            var (firstName, lastName) = dto.GetNames();

            user.FullName = fullName;
            user.PhoneNumber = dto.PhoneNumber.Trim();
            _unitOfWork.Repository<ApplicationUser>().Update(user);

            var customer = await _unitOfWork.Repository<Customer>()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (customer != null)
            {
                customer.FirstName = firstName;
                customer.LastName = lastName;
                customer.PhoneNumber = dto.PhoneNumber.Trim();
                customer.Address = dto.Address?.Trim();
                _unitOfWork.Repository<Customer>().Update(customer);
            }
            else
            {
                customer = new Customer
                {
                    UserId = user.Id,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email,
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    Address = dto.Address?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
            }

            // Note: Historical booking requests remain completely untouched to preserve historical data
            await _unitOfWork.CompleteAsync(cancellationToken);

            return await GetProfileAsync(userId, cancellationToken);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("The current password provided is incorrect.");
            }

            // Enforce password policy on new password
            ValidatePasswordPolicy(dto.NewPassword);

            user.PasswordHash = HashPassword(dto.NewPassword);
            _unitOfWork.Repository<ApplicationUser>().Update(user);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return true;
        }

        public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _unitOfWork.Repository<ApplicationUser>().GetAllAsync(cancellationToken);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(id, cancellationToken);
            if (user == null) return false;

            user.IsActive = isActive;
            _unitOfWork.Repository<ApplicationUser>().Update(user);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }

        private static void ValidatePasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                throw new InvalidOperationException("Password must be at least 6 characters long.");
            }
        }

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            if (storedHash == password) return true;
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hash = Convert.FromBase64String(parts[1]);
                byte[] checkHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations: 100000,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    outputLength: 32);
                return CryptographicOperations.FixedTimeEquals(hash, checkHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
