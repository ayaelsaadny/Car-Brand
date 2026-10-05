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
    [Tags("Services")]
    public class ServicesController : BaseApiController
    {
        private readonly IServiceManagementService _serviceManagementService;
        private readonly IVehicleService _vehicleService;

        public ServicesController(
            IServiceManagementService serviceManagementService,
            IVehicleService vehicleService)
        {
            _serviceManagementService = serviceManagementService;
            _vehicleService = vehicleService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceDto>>>> GetAll(
            [FromQuery] ServiceFilterQueryDto query,
            CancellationToken cancellationToken = default)
        {
            // Public website browsing: only active services under active categories
            query.ActiveOnly = true;
            var services = await _serviceManagementService.GetAllAsync(query, cancellationToken);
            return HandleResult(services);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ServiceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<ServiceDto>>> GetById(int id, CancellationToken cancellationToken)
        {
            var service = await _serviceManagementService.GetByIdAsync(id, activeOnly: true, cancellationToken);
            return HandleResult(service);
        }

        [HttpGet("{id}/available-vehicles")]
        [ProducesResponseType(typeof(ApiResponse<VehicleAvailabilityResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<VehicleAvailabilityResponseDto>>> GetAvailableVehiclesForService(
            int id,
            [FromQuery] DateTime pickupDateTime,
            [FromQuery] DateTime? returnDateTime,
            [FromQuery] int numberOfPassengers = 1,
            [FromQuery] int numberOfLuggage = 0,
            CancellationToken cancellationToken = default)
        {
            var service = await _serviceManagementService.GetByIdAsync(id, activeOnly: true, cancellationToken);
            if (service == null)
            {
                return NotFound(new ApiResponse(404, $"Service with ID {id} not found or is currently inactive."));
            }

            var query = new AvailableVehiclesQueryDto
            {
                ServiceId = id,
                PickupDateTime = pickupDateTime,
                ReturnDateTime = returnDateTime,
                NumberOfPassengers = numberOfPassengers,
                NumberOfLuggage = numberOfLuggage
            };

            var result = await _vehicleService.GetAvailableVehiclesAsync(query, cancellationToken);
            return HandleResult(result, "Available vehicles retrieved successfully");
        }
    }
}
