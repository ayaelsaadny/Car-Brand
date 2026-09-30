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
    }
}
