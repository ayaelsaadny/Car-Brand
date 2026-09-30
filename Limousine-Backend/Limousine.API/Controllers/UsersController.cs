using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseApiController
    {
        private readonly IAuthService _authService;

        public UsersController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetAll(CancellationToken cancellationToken)
        {
            var users = await _authService.GetAllUsersAsync(cancellationToken);
            return HandleResult(users);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _authService.GetUserByIdAsync(id, cancellationToken);
            return HandleResult(user);
        }

        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> ToggleStatus(int id, [FromBody] ToggleUserStatusDto dto, CancellationToken cancellationToken)
        {
            var success = await _authService.ToggleUserStatusAsync(id, dto.IsActive, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"User with ID {id} not found.");
            }

            var statusText = dto.IsActive ? "activated" : "deactivated";
            return HandleSuccess($"User account {statusText} successfully.");
        }
    }
}
