using Limousine.Core.Entities;
using Limousine.Core.Interfaces.Repositories;
using Limousine.Infrastracture.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Infrastracture.Repositories
{
    public class BookingRequestRepository : GenericRepository<BookingRequest>, IBookingRequestRepository
    {
        public BookingRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<BookingRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Images)
                .Include(b => b.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<BookingRequest?> GetByReferenceWithDetailsAsync(string bookingReference, CancellationToken cancellationToken = default)
        {
            return await _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Images)
                .Include(b => b.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference, cancellationToken);
        }

        public async Task<IReadOnlyList<BookingRequest>> GetByCustomerIdWithDetailsAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Images)
                .Include(b => b.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<BookingRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Images)
                .Include(b => b.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<BookingRequest> Items, int TotalCount)> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default)
        {
            var query = _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.Vehicle)
                    .ThenInclude(v => v.Images)
                .Include(b => b.StatusHistories.OrderByDescending(h => h.ChangedAt))
                .AsQueryable();

            // 1. General Search Term (matches Name, Email, Phone, Booking ID, Booking Reference)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                bool isNumeric = int.TryParse(s, out int searchId);

                query = query.Where(b =>
                    (isNumeric && b.Id == searchId) ||
                    b.BookingReference.Contains(s) ||
                    b.CustomerFullName.Contains(s) ||
                    b.CustomerEmail.Contains(s) ||
                    b.CustomerPhoneNumber.Contains(s) ||
                    (b.Customer != null && (
                        b.Customer.FirstName.Contains(s) ||
                        b.Customer.LastName.Contains(s) ||
                        b.Customer.Email.Contains(s) ||
                        b.Customer.PhoneNumber.Contains(s))));
            }

            // 2. Specific Search Fields
            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                var name = filter.CustomerName.Trim();
                query = query.Where(b =>
                    b.CustomerFullName.Contains(name) ||
                    (b.Customer != null && (
                        b.Customer.FirstName.Contains(name) ||
                        b.Customer.LastName.Contains(name))));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerEmail))
            {
                var email = filter.CustomerEmail.Trim();
                query = query.Where(b =>
                    b.CustomerEmail.Contains(email) ||
                    (b.Customer != null && b.Customer.Email.Contains(email)));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerPhone))
            {
                var phone = filter.CustomerPhone.Trim();
                query = query.Where(b =>
                    b.CustomerPhoneNumber.Contains(phone) ||
                    (b.Customer != null && b.Customer.PhoneNumber.Contains(phone)));
            }

            if (filter.BookingId.HasValue && filter.BookingId.Value > 0)
            {
                query = query.Where(b => b.Id == filter.BookingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.BookingReference))
            {
                var reference = filter.BookingReference.Trim();
                query = query.Where(b => b.BookingReference.Contains(reference));
            }

            // 3. Filters
            if (filter.Status.HasValue)
            {
                query = query.Where(b => b.Status == filter.Status.Value);
            }

            if (filter.ServiceId.HasValue && filter.ServiceId.Value > 0)
            {
                query = query.Where(b => b.ServiceId == filter.ServiceId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            {
                var sName = filter.ServiceName.Trim();
                query = query.Where(b =>
                    b.ServiceNameSnapshot.Contains(sName) ||
                    (b.Service != null && b.Service.Name.Contains(sName)));
            }

            if (filter.VehicleId.HasValue && filter.VehicleId.Value > 0)
            {
                query = query.Where(b => b.VehicleId == filter.VehicleId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(filter.VehicleName))
            {
                var vName = filter.VehicleName.Trim();
                query = query.Where(b =>
                    b.VehicleNameSnapshot.Contains(vName) ||
                    (b.Vehicle != null && (
                        b.Vehicle.Name.Contains(vName) ||
                        b.Vehicle.Make.Contains(vName) ||
                        b.Vehicle.Model.Contains(vName))));
            }

            // Date Filters
            if (filter.Date.HasValue)
            {
                var targetDate = filter.Date.Value.Date;
                var nextDay = targetDate.AddDays(1);
                query = query.Where(b => b.PickupDateTime >= targetDate && b.PickupDateTime < nextDay);
            }
            else
            {
                if (filter.FromDate.HasValue)
                {
                    var from = filter.FromDate.Value.Date;
                    query = query.Where(b => b.PickupDateTime >= from);
                }

                if (filter.ToDate.HasValue)
                {
                    var to = filter.ToDate.Value.Date.AddDays(1);
                    query = query.Where(b => b.PickupDateTime < to);
                }
            }

            // 4. Sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                var sortByLower = filter.SortBy.Trim().ToLowerInvariant();
                query = sortByLower switch
                {
                    "date" or "pickupdate" or "pickupdatetime" => filter.SortDescending
                        ? query.OrderByDescending(b => b.PickupDateTime)
                        : query.OrderBy(b => b.PickupDateTime),
                    "status" => filter.SortDescending
                        ? query.OrderByDescending(b => b.Status)
                        : query.OrderBy(b => b.Status),
                    "price" or "estimatedprice" => filter.SortDescending
                        ? query.OrderByDescending(b => b.EstimatedPrice)
                        : query.OrderBy(b => b.EstimatedPrice),
                    "customername" => filter.SortDescending
                        ? query.OrderByDescending(b => b.CustomerFullName)
                        : query.OrderBy(b => b.CustomerFullName),
                    "id" => filter.SortDescending
                        ? query.OrderByDescending(b => b.Id)
                        : query.OrderBy(b => b.Id),
                    _ => filter.SortDescending
                        ? query.OrderByDescending(b => b.CreatedAt)
                        : query.OrderBy(b => b.CreatedAt)
                };
            }
            else
            {
                query = filter.SortDescending
                    ? query.OrderByDescending(b => b.CreatedAt)
                    : query.OrderBy(b => b.CreatedAt);
            }

            // 5. Total Count
            var totalCount = await query.CountAsync(cancellationToken);

            // 6. Pagination
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
