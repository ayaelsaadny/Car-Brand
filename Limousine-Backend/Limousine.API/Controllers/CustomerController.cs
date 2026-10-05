using Limousine.API.Errors;
using Limousine.API.Extensions;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Route("api/customers")]
    public class CustomerController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IBookingService _bookingService;

        public CustomerController(IAuthService authService, IBookingService bookingService)
        {
            _authService = authService;
            _bookingService = bookingService;
        }

        [HttpGet("profile")]
        [HttpGet("me")]
        [HttpGet("me/profile")]
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
        [HttpPut("me")]
        [HttpPut("me/profile")]
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

        [HttpGet("me/bookings")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BookingRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingRequestDto>>>> GetMyBookings(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            var bookings = await _bookingService.GetUserBookingsAsync(userId.Value, cancellationToken);
            return HandleResult(bookings, "Customer bookings retrieved successfully");
        }

        [HttpGet("me/bookings/{id}")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetMyBookingById(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            // Strictly enforce customer ownership: pass isAdmin: false so customer can only access their own booking
            var booking = await _bookingService.GetBookingByIdAsync(id, userId.Value, isAdmin: false, cancellationToken);
            if (booking == null)
            {
                return NotFound(new ApiResponse(404, $"Booking with ID {id} not found."));
            }

            return HandleResult(booking, "Booking details retrieved successfully");
        }
    }
}
