using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    public class SettingsController : BaseApiController
    {
        private readonly ISettingService _settingService;

        public SettingsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WebsiteSettingDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<WebsiteSettingDto>>>> GetAll(CancellationToken cancellationToken)
        {
            var settings = await _settingService.GetAllSettingsAsync(cancellationToken);
            return HandleResult(settings);
        }

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(ApiResponse<WebsiteSettingDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<WebsiteSettingDto>>> GetByKey(string key, CancellationToken cancellationToken)
        {
            var setting = await _settingService.GetSettingByKeyAsync(key, cancellationToken);
            return HandleResult(setting);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{key}")]
        [ProducesResponseType(typeof(ApiResponse<WebsiteSettingDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<WebsiteSettingDto>>> Update(string key, [FromBody] UpdateWebsiteSettingDto dto, CancellationToken cancellationToken)
        {
            var updated = await _settingService.UpdateSettingAsync(key, dto.Value, cancellationToken);
            return HandleResult(updated, "Setting updated successfully");
        }
    }
}
