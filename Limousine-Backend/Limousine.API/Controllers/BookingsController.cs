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
    public class BookingsController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> Create([FromBody] CreateBookingRequestDto dto, CancellationToken cancellationToken)
        {
            var authenticatedUserId = User.GetUserId();
            var booking = await _bookingService.CreateBookingAsync(dto, authenticatedUserId, cancellationToken);
            return HandleCreated(booking, "Booking request submitted successfully");
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
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetByReference(string reference, CancellationToken cancellationToken)
        {
            var booking = await _bookingService.GetBookingByReferenceAsync(reference, cancellationToken);
            return HandleResult(booking);
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var isAdmin = User.IsAdmin();

            var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin, cancellationToken);
            return HandleResult(booking);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BookingRequestDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingRequestDto>>>> GetAll(CancellationToken cancellationToken)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(cancellationToken);
            return HandleResult(bookings);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto, CancellationToken cancellationToken)
        {
            var changedBy = User.GetUserEmail() ?? "Admin";
            var updated = await _bookingService.UpdateBookingStatusAsync(id, dto, changedBy, cancellationToken);
            return HandleResult(updated, $"Booking status updated to {dto.Status}");
        }
    }
}
