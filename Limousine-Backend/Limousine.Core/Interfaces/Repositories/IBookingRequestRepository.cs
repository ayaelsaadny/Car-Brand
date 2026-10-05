using Limousine.Core.Entities;
using Limousine.Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Core.Interfaces.Repositories
{
    public class BookingSearchFilter
    {
        public string? Search { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public int? BookingId { get; set; }
        public string? BookingReference { get; set; }
        public BookingStatus? Status { get; set; }
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public int? VehicleId { get; set; }
        public string? VehicleName { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public interface IBookingRequestRepository : IGenericRepository<BookingRequest>
    {
        Task<BookingRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<BookingRequest?> GetByReferenceWithDetailsAsync(string bookingReference, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequest>> GetByCustomerIdWithDetailsAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<BookingRequest> Items, int TotalCount)> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default);
    }
}
