using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto?> AdminLoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task<UserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<CustomerProfileDto?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
        Task<CustomerProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ToggleUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    }
}
