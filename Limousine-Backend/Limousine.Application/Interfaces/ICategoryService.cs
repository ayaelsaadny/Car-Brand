using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool? activeOnly = null, bool includeServices = false, CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetByIdAsync(int id, bool? activeOnly = null, bool includeServices = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceDto>> GetServicesByCategoryIdAsync(int categoryId, bool? activeOnly = null, CancellationToken cancellationToken = default);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task<CategoryDto?> UpdateDisplayOrderAsync(int id, int displayOrder, CancellationToken cancellationToken = default);
        Task<CategoryDto?> UploadImageAsync(int id, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default);
    }
}
