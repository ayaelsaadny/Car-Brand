using Limousine.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Core.Interfaces.Repositories
{
    public interface IBookingRequestRepository : IGenericRepository<BookingRequest>
    {
        Task<BookingRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<BookingRequest?> GetByReferenceWithDetailsAsync(string bookingReference, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequest>> GetByCustomerIdWithDetailsAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BookingRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    }
}
