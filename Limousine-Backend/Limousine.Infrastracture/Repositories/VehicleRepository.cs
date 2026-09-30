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
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Vehicle?> GetDefaultVehicleAsync(CancellationToken cancellationToken = default)
        {
            // Look for vehicle marked IsDefault and IsActive
            var defaultVehicle = await _context.Vehicles
                .Include(v => v.Images.OrderBy(img => img.DisplayOrder))
                .FirstOrDefaultAsync(v => v.IsDefault && v.IsActive, cancellationToken);

            // Fallback: If no vehicle explicitly marked default, pick the first active vehicle
            if (defaultVehicle == null)
            {
                defaultVehicle = await _context.Vehicles
                    .Include(v => v.Images.OrderBy(img => img.DisplayOrder))
                    .FirstOrDefaultAsync(v => v.IsActive, cancellationToken);
            }

            return defaultVehicle;
        }

        public async Task<Vehicle?> GetVehicleWithImagesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Vehicles
                .Include(v => v.Images.OrderBy(img => img.DisplayOrder))
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Vehicle>> GetAllWithImagesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            var query = _context.Vehicles
                .Include(v => v.Images.OrderBy(img => img.DisplayOrder))
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(v => v.IsActive);
            }

            return await query.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Name).ToListAsync(cancellationToken);
        }

        public async Task SetDefaultVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
        {
            var allVehicles = await _context.Vehicles.ToListAsync(cancellationToken);
            foreach (var vehicle in allVehicles)
            {
                vehicle.IsDefault = (vehicle.Id == vehicleId);
            }
        }
    }
}
