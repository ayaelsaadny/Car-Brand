using Limousine.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Core.Interfaces.Repositories
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<Vehicle?> GetDefaultVehicleAsync(CancellationToken cancellationToken = default);
        Task<Vehicle?> GetVehicleWithImagesAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Vehicle>> GetAllWithImagesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
        Task SetDefaultVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);
    }
}
