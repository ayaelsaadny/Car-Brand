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
    [Route("api/admin/vehicles")]
    [Tags("Admin - Vehicles")]
    public class AdminVehiclesController : BaseApiController
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IVehicleService _vehicleService;

        public AdminVehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleDto>>>> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken = default)
        {
            var vehicles = await _vehicleService.GetAllAsync(activeOnly ?? false, cancellationToken);
            return HandleResult(vehicles);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id, activeOnly: false, cancellationToken);
            return HandleResult(vehicle);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> Create([FromBody] CreateVehicleDto dto, CancellationToken cancellationToken)
        {
            var created = await _vehicleService.CreateAsync(dto, cancellationToken);
            return HandleCreated(created, "Vehicle added to fleet successfully");
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> Update(int id, [FromBody] UpdateVehicleDto dto, CancellationToken cancellationToken)
        {
            var updated = await _vehicleService.UpdateAsync(id, dto, cancellationToken);
            return HandleResult(updated, "Vehicle updated successfully");
        }

        [HttpPatch("{id}/status")]
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> UpdateStatus(int id, [FromBody] UpdateVehicleStatusDto dto, CancellationToken cancellationToken)
        {
            var updated = await _vehicleService.SetStatusAsync(id, dto.IsActive, cancellationToken);
            var statusLabel = dto.IsActive ? "activated" : "deactivated";
            return HandleResult(updated, $"Vehicle {statusLabel} successfully");
        }

        [HttpPost("{id}/default")]
        [HttpPut("{id}/default")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> SetDefault(int id, CancellationToken cancellationToken)
        {
            var success = await _vehicleService.SetDefaultAsync(id, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Vehicle with ID {id} not found.");
            }

            return HandleSuccess("Vehicle marked as default successfully");
        }

        [HttpPost("{id}/images/upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleImageDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleImageDto>>>> UploadImages(int id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new ApiResponse(400, "No image files were uploaded."));
            }

            foreach (var file in files)
            {
                if (file.Length > MaxImageSizeBytes)
                {
                    return BadRequest(new ApiResponse(400, $"File '{file.FileName}' exceeds the maximum size limit of {MaxImageSizeBytes / (1024 * 1024)}MB."));
                }

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !AllowedImageExtensions.Contains(ext))
                {
                    return BadRequest(new ApiResponse(400, $"File '{file.FileName}' has an invalid format. Allowed: .jpg, .jpeg, .png, .webp, .svg."));
                }
            }

            var streams = new List<(Stream Stream, string FileName)>();
            try
            {
                foreach (var file in files)
                {
                    streams.Add((file.OpenReadStream(), file.FileName));
                }

                var uploadedImages = await _vehicleService.UploadImagesAsync(id, streams, cancellationToken);
                return HandleResult(uploadedImages, "Vehicle images uploaded successfully");
            }
            finally
            {
                foreach (var item in streams)
                {
                    await item.Stream.DisposeAsync();
                }
            }
        }

        [HttpPut("{id}/images/{imageId}/primary")]
        [HttpPost("{id}/images/{imageId}/primary")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> SetPrimaryImage(int id, int imageId, CancellationToken cancellationToken)
        {
            var success = await _vehicleService.SetPrimaryImageAsync(id, imageId, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Image with ID {imageId} not found for vehicle {id}.");
            }

            return HandleSuccess("Primary vehicle image updated successfully");
        }

        [HttpPut("{id}/images/reorder")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> ReorderImages(int id, [FromBody] ReorderVehicleImagesDto dto, CancellationToken cancellationToken)
        {
            if (dto.Orders == null || dto.Orders.Count == 0)
            {
                return BadRequest(new ApiResponse(400, "Orders list cannot be empty."));
            }

            var success = await _vehicleService.ReorderImagesAsync(id, dto.Orders, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Vehicle with ID {id} not found.");
            }

            return HandleSuccess("Vehicle images reordered successfully");
        }

        [HttpDelete("images/{imageId}")]
        [HttpDelete("{id}/images/{imageId}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> DeleteImage(int imageId, [FromRoute] int? id = null, CancellationToken cancellationToken = default)
        {
            var success = id.HasValue
                ? await _vehicleService.DeleteImageAsync(id.Value, imageId, cancellationToken)
                : await _vehicleService.DeleteImageAsync(imageId, cancellationToken);

            if (!success)
            {
                return HandleNotFound($"Image with ID {imageId} not found.");
            }

            return HandleSuccess("Vehicle image removed successfully");
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> Delete(int id, [FromQuery] bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var success = await _vehicleService.DeleteAsync(id, archiveOnly: !hardDelete, cancellationToken);
            if (!success)
            {
                return HandleNotFound($"Vehicle with ID {id} not found.");
            }

            var actionLabel = hardDelete ? "permanently deleted" : "archived/deactivated";
            return HandleSuccess($"Vehicle {actionLabel} successfully");
        }
    }
}
