using Limousine.API.Errors;
using Limousine.API.Extensions;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/bookings")]
    [Tags("Admin - Bookings")]
    public class AdminBookingsController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public AdminBookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<BookingRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<PagedResult<BookingRequestDto>>>> GetAll(
            [FromQuery] BookingSearchFilterDto query, 
            CancellationToken cancellationToken)
        {
            var result = await _bookingService.SearchBookingsAsync(query, cancellationToken);
            return HandleResult(result, "Bookings retrieved successfully");
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BookingRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<BookingRequestDto>>>> GetAllUnpaged(
            [FromQuery] BookingStatus? status, 
            CancellationToken cancellationToken)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(status, cancellationToken);
            return HandleResult(bookings);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var userId = User.GetUserId();
            var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin: true, cancellationToken);
            return HandleResult(booking);
        }

        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var changedBy = User.GetUserEmail() ?? "Admin";
            var updated = await _bookingService.UpdateBookingStatusAsync(id, dto, changedBy, cancellationToken);
            return HandleResult(updated, $"Booking status updated to {dto.Status}");
        }

        [HttpPost("{id}/accept")]
        [HttpPut("{id}/accept")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> AcceptBooking(
            int id, 
            [FromBody] AcceptBookingDto? dto, 
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var changedBy = User.GetUserEmail() ?? "Admin";
            var updated = await _bookingService.AcceptBookingAsync(id, changedBy, dto, cancellationToken);
            return HandleResult(updated, "Booking request accepted successfully.");
        }

        [HttpPost("{id}/reject")]
        [HttpPut("{id}/reject")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> RejectBooking(
            int id, 
            [FromBody] RejectBookingDto dto, 
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var changedBy = User.GetUserEmail() ?? "Admin";
            var updated = await _bookingService.RejectBookingAsync(id, dto, changedBy, cancellationToken);
            return HandleResult(updated, "Booking request rejected.");
        }

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
            var changedBy = User.GetUserEmail() ?? "Admin";

            var updated = await _bookingService.CancelBookingAsync(id, dto, changedBy, userId, isAdmin: true, cancellationToken);
            return HandleResult(updated, "Booking cancelled successfully.");
        }

        [HttpPost("{id}/complete")]
        [HttpPut("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<BookingRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<BookingRequestDto>>> CompleteBooking(
            int id, 
            [FromBody] CompleteBookingDto? dto, 
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new ApiResponse(400, "Invalid booking ID."));
            }

            var changedBy = User.GetUserEmail() ?? "Admin";
            var updated = await _bookingService.CompleteBookingAsync(id, changedBy, dto, cancellationToken);
            return HandleResult(updated, "Booking marked as completed successfully.");
        }
    }
}
