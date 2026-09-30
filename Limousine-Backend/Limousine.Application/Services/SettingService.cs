using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class SettingService : ISettingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SettingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<WebsiteSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _unitOfWork.Repository<WebsiteSetting>().GetAllAsync(cancellationToken);
            return settings.Select(s => new WebsiteSettingDto
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.Value,
                Description = s.Description,
                Group = s.Group,
                UpdatedAt = s.UpdatedAt
            }).ToList();
        }

        public async Task<WebsiteSettingDto?> GetSettingByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            var setting = await _unitOfWork.Repository<WebsiteSetting>()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (setting == null) return null;

            return new WebsiteSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Group = setting.Group,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<WebsiteSettingDto?> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            var setting = await _unitOfWork.Repository<WebsiteSetting>()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (setting == null)
            {
                setting = new WebsiteSetting
                {
                    Key = key,
                    Value = value,
                    Group = "General",
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<WebsiteSetting>().AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<WebsiteSetting>().Update(setting);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            return new WebsiteSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Group = setting.Group,
                UpdatedAt = setting.UpdatedAt
            };
        }
    }
}
