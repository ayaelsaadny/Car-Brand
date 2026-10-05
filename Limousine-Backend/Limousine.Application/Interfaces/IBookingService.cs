using Limousine.Application.DTOs;
using Limousine.Core.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingRequestDto> CreateBookingAsync(CreateBookingRequestDto dto, int? authenticatedUserId = null, CancellationToken cancellationToken = default);
        Task<BookingRequestDto> CreateGuestBookingAsync(GuestBookingRequestDto dto, CancellationToken cancellationToken = default);
        Task<BookingFormProfileDto?> GetBookingFormProfileAsync(int authenticatedUserId, CancellationToken cancellationToken = default);
        Task<BookingFormInitDto> GetBookingFormInitAsync(int? authenticatedUserId, int? serviceId = null, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> GetBookingByIdAsync(int id, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequestDto>> GetUserBookingsAsync(int authenticatedUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequestDto>> GetAllBookingsAsync(BookingStatus? status = null, CancellationToken cancellationToken = default);
        Task<PagedResult<BookingRequestDto>> SearchBookingsAsync(BookingSearchFilterDto filter, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto dto, string changedBy, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> AcceptBookingAsync(int id, string changedBy, AcceptBookingDto? dto = null, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> RejectBookingAsync(int id, RejectBookingDto dto, string changedBy, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> CancelBookingAsync(int id, CancelBookingDto dto, string changedBy, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> CompleteBookingAsync(int id, string changedBy, CompleteBookingDto? dto = null, CancellationToken cancellationToken = default);
    }
}
