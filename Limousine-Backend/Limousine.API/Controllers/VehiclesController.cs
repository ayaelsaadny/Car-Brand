using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [Route("api/[controller]")]
    [Tags("Vehicles")]
    public class VehiclesController : BaseApiController
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleDto>>>> GetAll(CancellationToken cancellationToken = default)
        {
            var vehicles = await _vehicleService.GetAllAsync(activeOnly: true, cancellationToken);
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
            var vehicle = await _vehicleService.GetByIdAsync(id, activeOnly: true, cancellationToken);
            return HandleResult(vehicle);
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
    }
}
