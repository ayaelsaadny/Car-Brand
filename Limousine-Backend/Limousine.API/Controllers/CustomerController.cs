using Limousine.API.Errors;
using Limousine.API.Extensions;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class CustomerController : BaseApiController
    {
        private readonly IAuthService _authService;

        public CustomerController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<CustomerProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CustomerProfileDto>>> GetProfile(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            var profile = await _authService.GetProfileAsync(userId.Value, cancellationToken);
            return HandleResult(profile);
        }

        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<CustomerProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<ActionResult<ApiResponse<CustomerProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            var updatedProfile = await _authService.UpdateProfileAsync(userId.Value, dto, cancellationToken);
            return HandleResult(updatedProfile, "Profile updated successfully");
        }
    }
}
