using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [Route("api/[controller]")]
    [Tags("Categories")]
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAll(
            [FromQuery] bool includeServices = false,
            CancellationToken cancellationToken = default)
        {
            var categories = await _categoryService.GetAllAsync(activeOnly: true, includeServices, cancellationToken);
            return HandleResult(categories);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(
            int id,
            [FromQuery] bool includeServices = false,
            CancellationToken cancellationToken = default)
        {
            var category = await _categoryService.GetByIdAsync(id, activeOnly: true, includeServices, cancellationToken);
            return HandleResult(category);
        }

        [HttpGet("{id}/services")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceDto>>>> GetCategoryServices(
            int id,
            CancellationToken cancellationToken = default)
        {
            var services = await _categoryService.GetServicesByCategoryIdAsync(id, activeOnly: true, cancellationToken);
            return HandleResult(services);
        }
    }
}
