using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    public class VehiclesController : BaseApiController
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleDto>>>> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken = default)
        {
            var isAdmin = User.IsInRole("Admin");
            var effectiveActiveOnly = !isAdmin || (activeOnly ?? false);

            var vehicles = await _vehicleService.GetAllAsync(effectiveActiveOnly, cancellationToken);
            return HandleResult(vehicles);
        }

        [HttpGet("default")]
        [ProducesResponseType(typeof(ApiResponse<DefaultVehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<DefaultVehicleDto>>> GetDefaultVehicle(
            [FromQuery] DateTime? pickupDateTime,
            [FromQuery] DateTime? returnDateTime,
            CancellationToken cancellationToken)
        {
            var vehicle = await _vehicleService.GetDefaultVehicleWithAvailabilityAsync(pickupDateTime, returnDateTime, cancellationToken);
            return HandleResult(vehicle, "Default vehicle retrieved");
        }

        [HttpGet("available")]
        [ProducesResponseType(typeof(ApiResponse<VehicleAvailabilityResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<VehicleAvailabilityResponseDto>>> GetAvailableVehicles(
            [FromQuery] AvailableVehiclesQueryDto query,
            CancellationToken cancellationToken)
        {
            var result = await _vehicleService.GetAvailableVehiclesAsync(query, cancellationToken);
            return HandleResult(result, "Available vehicles evaluated successfully");
        }

        [HttpPost("available")]
        [ProducesResponseType(typeof(ApiResponse<VehicleAvailabilityResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<VehicleAvailabilityResponseDto>>> QueryAvailableVehicles(
            [FromBody] AvailableVehiclesQueryDto query,
            CancellationToken cancellationToken)
        {
            var result = await _vehicleService.GetAvailableVehiclesAsync(query, cancellationToken);
            return HandleResult(result, "Available vehicles evaluated successfully");
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var isAdmin = User.IsInRole("Admin");
            var vehicle = await _vehicleService.GetByIdAsync(id, !isAdmin, cancellationToken);
            return HandleResult(vehicle);
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [HttpGet("{id}/images")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleImageDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleImageDto>>>> GetImages(int id, CancellationToken cancellationToken)
        {
            var images = await _vehicleService.GetVehicleImagesAsync(id, cancellationToken);
            return HandleResult(images);
        }

        [HttpGet("{id}/images/primary")]
        [ProducesResponseType(typeof(ApiResponse<VehicleImageDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleImageDto>>> GetPrimaryImage(int id, CancellationToken cancellationToken)
        {
            var image = await _vehicleService.GetPrimaryImageAsync(id, cancellationToken);
            if (image == null)
            {
                return NotFound(new ApiResponse(404, $"No primary image found for vehicle {id}."));
            }

            return HandleResult(image, "Primary vehicle image retrieved");
        }

        [Authorize(Roles = "Admin")]
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

                var addedImages = await _vehicleService.UploadImagesAsync(id, streams, cancellationToken);
                return HandleResult(addedImages, $"{addedImages.Count} image(s) uploaded successfully");
            }
            finally
            {
                foreach (var (stream, _) in streams)
                {
                    stream.Dispose();
                }
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/images")]
        [ProducesResponseType(typeof(ApiResponse<VehicleImageDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleImageDto>>> AddImage(int id, [FromBody] CreateVehicleImageDto dto, CancellationToken cancellationToken)
        {
            var image = await _vehicleService.AddImageAsync(id, dto, cancellationToken);
            return HandleCreated(image, "Image added to vehicle gallery");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/images/{imageId}/primary")]
        [HttpPatch("{id}/images/{imageId}/primary")]
        [HttpPut("{id}/images/{imageId}/main")]
        [HttpPatch("{id}/images/{imageId}/main")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
