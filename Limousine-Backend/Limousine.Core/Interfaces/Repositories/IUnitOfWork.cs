using Limousine.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Core.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : BaseEntitiy;
        IVehicleRepository Vehicles { get; }
        IBookingRequestRepository BookingRequests { get; }
        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
    }
}
