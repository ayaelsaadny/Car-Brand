using Limousine.API.Errors;
using Limousine.API.Extensions;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(dto, cancellationToken);
            if (response == null)
            {
                return Unauthorized(new ApiResponse(401, "Invalid email or password."));
            }

            return HandleResult(response, "Login successful");
        }

        [HttpPost("admin/login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> AdminLogin([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authService.AdminLoginAsync(dto, cancellationToken);
                if (response == null)
                {
                    return Unauthorized(new ApiResponse(401, "Invalid email or password."));
                }

                return HandleResult(response, "Admin login successful");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
        {
            var response = await _authService.RegisterAsync(dto, cancellationToken);
            return HandleCreated(response, "Registration successful");
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            var user = await _authService.GetCurrentUserAsync(userId.Value, cancellationToken);
            return HandleResult(user);
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public ActionResult<ApiResponse> Logout()
        {
            return HandleSuccess("Logged out successfully");
        }
    }
}
