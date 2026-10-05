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
    [Route("api/[controller]")]
    [Tags("Bookings")]
    public class BookingsController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> Create([FromBody] CreateBookingRequestDto dto, CancellationToken cancellationToken)
        {
            var authenticatedUserId = User.GetUserId();
            var booking = await _bookingService.CreateBookingAsync(dto, authenticatedUserId, cancellationToken);
            return HandleCreated(booking, "Booking request submitted successfully");
        }

        [AllowAnonymous]
        [HttpPost("guest")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> CreateGuestBooking([FromBody] GuestBookingRequestDto dto, CancellationToken cancellationToken)
        {
            var booking = await _bookingService.CreateGuestBookingAsync(dto, cancellationToken);
            return HandleCreated(booking, "Guest booking request submitted successfully");
        }

        [AllowAnonymous]
        [HttpGet("form-init")]
        [ProducesResponseType(typeof(ApiResponse<BookingFormInitDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<BookingFormInitDto>>> GetBookingFormInit(
            [FromQuery] int? serviceId,
            CancellationToken cancellationToken)
        {
            if (serviceId.HasValue && serviceId.Value <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid service ID."));
            }

            var userId = User.GetUserId();
            var initData = await _bookingService.GetBookingFormInitAsync(userId, serviceId, cancellationToken);
            return HandleResult(initData, "Booking form initialization data retrieved successfully");
        }

        [Authorize]
        [HttpGet("form-profile")]
        [HttpGet("prefill")]
        [ProducesResponseType(typeof(ApiResponse<BookingFormProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingFormProfileDto>>> GetBookingFormProfile(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse(401, "User ID not found in claims."));
            }

            var profile = await _bookingService.GetBookingFormProfileAsync(userId.Value, cancellationToken);
            return HandleResult(profile, "Booking form customer profile retrieved successfully");
        }

        [Authorize]
        [HttpGet("my")]
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
            return HandleResult(bookings);
        }

        [HttpGet("reference/{reference}")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetByReference(string reference, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return BadRequest(new ApiResponse(400, "Booking reference is required."));
            }

            var booking = await _bookingService.GetBookingByReferenceAsync(reference, cancellationToken);
            return HandleResult(booking);
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var userId = User.GetUserId();
            var isAdmin = User.IsAdmin();

            var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin, cancellationToken);
            return HandleResult(booking);
        }

        [Authorize]
        [HttpPost("{id}/cancel")]
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> CancelBooking(
            int id, 
            [FromBody] CancelBookingDto dto, 
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var userId = User.GetUserId();
            var isAdmin = User.IsAdmin();
            var changedBy = User.GetUserEmail() ?? (isAdmin ? "Admin" : "Customer");

            var updated = await _bookingService.CancelBookingAsync(id, dto, changedBy, userId, isAdmin, cancellationToken);
            return HandleResult(updated, "Booking cancelled successfully.");
        }
    }
}
