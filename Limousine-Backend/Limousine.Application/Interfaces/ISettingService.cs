using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface ISettingService
    {
        Task<IReadOnlyList<WebsiteSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
        Task<WebsiteSettingDto?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<WebsiteSettingDto?> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default);
    }
}
