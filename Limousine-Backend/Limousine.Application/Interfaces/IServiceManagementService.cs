using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IServiceManagementService
    {
        Task<IReadOnlyList<ServiceDto>> GetAllAsync(int? categoryId = null, string? searchTerm = null, bool? activeOnly = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceDto>> GetAllAsync(ServiceFilterQueryDto filter, CancellationToken cancellationToken = default);
        Task<ServiceDto?> GetByIdAsync(int id, bool? activeOnly = null, CancellationToken cancellationToken = default);
        Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default);
        Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto, CancellationToken cancellationToken = default);
        Task<ServiceDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<ServiceDto?> UpdateDisplayOrderAsync(int id, int displayOrder, CancellationToken cancellationToken = default);
        Task<ServiceDto?> UploadImageAsync(int id, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default);
    }
}
