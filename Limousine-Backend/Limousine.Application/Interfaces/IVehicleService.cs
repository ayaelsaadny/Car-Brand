using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IVehicleService
    {
        Task<IReadOnlyList<VehicleDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
        Task<VehicleDto?> GetByIdAsync(int id, bool? activeOnly = null, CancellationToken cancellationToken = default);
        Task<VehicleDto?> GetDefaultVehicleAsync(CancellationToken cancellationToken = default);
        Task<DefaultVehicleDto?> GetDefaultVehicleWithAvailabilityAsync(DateTime? pickupDateTime = null, DateTime? returnDateTime = null, CancellationToken cancellationToken = default);
        Task<VehicleDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default);
        Task<VehicleDto?> UpdateAsync(int id, UpdateVehicleDto dto, CancellationToken cancellationToken = default);
        Task<VehicleDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<bool> SetDefaultAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VehicleImageDto>> GetVehicleImagesAsync(int vehicleId, CancellationToken cancellationToken = default);
        Task<VehicleImageDto?> GetPrimaryImageAsync(int vehicleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VehicleImageDto>> UploadImagesAsync(int vehicleId, IEnumerable<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default);
        Task<VehicleImageDto> AddImageAsync(int vehicleId, CreateVehicleImageDto dto, CancellationToken cancellationToken = default);
        Task<bool> SetPrimaryImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default);
        Task<bool> SetMainImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default);
        Task<bool> ReorderImagesAsync(int vehicleId, List<VehicleImageOrderDto> orders, CancellationToken cancellationToken = default);
        Task<bool> DeleteImageAsync(int imageId, CancellationToken cancellationToken = default);
        Task<bool> DeleteImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default);
        Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime? returnDateTime, int? excludeBookingId = null, CancellationToken cancellationToken = default);
        Task<VehicleAvailabilityResponseDto> GetAvailableVehiclesAsync(AvailableVehiclesQueryDto query, CancellationToken cancellationToken = default);
    }
}
