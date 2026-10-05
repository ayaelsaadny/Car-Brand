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
    [Route("api/admin/categories")]
    [Tags("Admin - Categories")]
    public class AdminCategoriesController : BaseApiController
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly ICategoryService _categoryService;

        public AdminCategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAll(
            [FromQuery] bool? activeOnly,
            [FromQuery] bool includeServices = false,
            CancellationToken cancellationToken = default)
        {
            var categories = await _categoryService.GetAllAsync(activeOnly ?? false, includeServices, cancellationToken);
            return HandleResult(categories);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(
            int id,
            [FromQuery] bool includeServices = false,
            CancellationToken cancellationToken = default)
        {
            var category = await _categoryService.GetByIdAsync(id, activeOnly: false, includeServices, cancellationToken);
            return HandleResult(category);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryDto dto, CancellationToken cancellationToken)
        {
            var created = await _categoryService.CreateAsync(dto, cancellationToken);
            return HandleCreated(created, "Category created successfully");
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(int id, [FromBody] UpdateCategoryDto dto, CancellationToken cancellationToken)
        {
            var updated = await _categoryService.UpdateAsync(id, dto, cancellationToken);
            return HandleResult(updated, "Category updated successfully");
        }

        [HttpPatch("{id}/status")]
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateStatus(int id, [FromBody] UpdateCategoryStatusDto dto, CancellationToken cancellationToken)
        {
            var updated = await _categoryService.SetStatusAsync(id, dto.IsActive, cancellationToken);
            var statusLabel = dto.IsActive ? "activated" : "deactivated";
            return HandleResult(updated, $"Category {statusLabel} successfully");
        }

        [HttpPatch("{id}/display-order")]
        [HttpPut("{id}/display-order")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateDisplayOrder(int id, [FromBody] UpdateCategoryDisplayOrderDto dto, CancellationToken cancellationToken)
        {
            var updated = await _categoryService.UpdateDisplayOrderAsync(id, dto.DisplayOrder, cancellationToken);
            return HandleResult(updated, "Display order updated successfully");
        }

        [HttpPost("{id}/image")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
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
            var updated = await _categoryService.UploadImageAsync(id, stream, file.FileName, cancellationToken);
            return HandleResult(updated, "Category image uploaded successfully");
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> Delete(int id, [FromQuery] bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var success = await _categoryService.DeleteAsync(id, archiveOnly: !hardDelete, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Category with ID {id} not found.");
            }

            var actionLabel = hardDelete ? "permanently deleted" : "archived/deactivated";
            return HandleSuccess($"Category {actionLabel} successfully");
        }
    }
}
