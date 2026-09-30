using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingRequestDto> CreateBookingAsync(CreateBookingRequestDto dto, int? authenticatedUserId = null, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> GetBookingByIdAsync(int id, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequestDto>> GetUserBookingsAsync(int authenticatedUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequestDto>> GetAllBookingsAsync(CancellationToken cancellationToken = default);
        Task<BookingRequestDto?> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto dto, string changedBy, CancellationToken cancellationToken = default);
    }
}
