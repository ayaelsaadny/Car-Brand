using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/services")]
    [Tags("Admin - Services")]
    public class AdminServicesController : BaseApiController
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IServiceManagementService _serviceManagementService;

        public AdminServicesController(IServiceManagementService serviceManagementService)
        {
            _serviceManagementService = serviceManagementService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceDto>>>> GetAll(
            [FromQuery] ServiceFilterQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var services = await _serviceManagementService.GetAllAsync(query, cancellationToken);
            return HandleResult(services);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var service = await _serviceManagementService.GetByIdAsync(id, activeOnly: false, cancellationToken);
            return HandleResult(service);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> Create([FromBody] CreateServiceDto dto, CancellationToken cancellationToken)
        {
            var created = await _serviceManagementService.CreateAsync(dto, cancellationToken);
            return HandleCreated(created, "Service created successfully");
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> Update(int id, [FromBody] UpdateServiceDto dto, CancellationToken cancellationToken)
        {
            var updated = await _serviceManagementService.UpdateAsync(id, dto, cancellationToken);
            return HandleResult(updated, "Service updated successfully");
        }

        [HttpPatch("{id}/status")]
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> UpdateStatus(int id, [FromBody] UpdateServiceStatusDto dto, CancellationToken cancellationToken)
        {
            var updated = await _serviceManagementService.SetStatusAsync(id, dto.IsActive, cancellationToken);
            var statusLabel = dto.IsActive ? "activated" : "deactivated";
            return HandleResult(updated, $"Service {statusLabel} successfully");
        }

        [HttpPatch("{id}/display-order")]
        [HttpPut("{id}/display-order")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> UpdateDisplayOrder(int id, [FromBody] UpdateServiceDisplayOrderDto dto, CancellationToken cancellationToken)
        {
            var updated = await _serviceManagementService.UpdateDisplayOrderAsync(id, dto.DisplayOrder, cancellationToken);
            return HandleResult(updated, "Display order updated successfully");
        }

        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse(400, "No image file was uploaded."));
            }

            if (file.Length > MaxImageSizeBytes)
            {
                return BadRequest(new ApiResponse(400, $"Image file size exceeds the maximum limit of {MaxImageSizeBytes / (1024 * 1024)}MB."));
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest(new ApiResponse(400, "Invalid image format. Allowed formats: .jpg, .jpeg, .png, .webp, .svg."));
            }

            using var stream = file.OpenReadStream();
            var updated = await _serviceManagementService.UploadImageAsync(id, stream, file.FileName, cancellationToken);
            return HandleResult(updated, "Service image uploaded successfully");
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> Delete(int id, [FromQuery] bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var success = await _serviceManagementService.DeleteAsync(id, archiveOnly: !hardDelete, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Service with ID {id} not found.");
            }

            var actionLabel = hardDelete ? "permanently deleted" : "archived/deactivated";
            return HandleSuccess($"Service {actionLabel} successfully");
        }
    }
}
