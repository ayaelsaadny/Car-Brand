using Limousine.API.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Limousine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected ActionResult<ApiResponse<T>> HandleResult<T>(T? result, string? successMessage = null)
        {
            if (result == null)
            {
                return NotFound(new ApiResponse(404, "Requested resource not found"));
            }

            return Ok(ApiResponse<T>.Success(result, successMessage));
        }

        protected ActionResult<ApiResponse<T>> HandleCreated<T>(T result, string? successMessage = null)
        {
            return StatusCode(201, ApiResponse<T>.Created(result, successMessage));
        }

        protected ActionResult<ApiResponse> HandleSuccess(string message = "Operation completed successfully")
        {
            return Ok(new ApiResponse(200, message));
        }

        protected ActionResult<ApiResponse> HandleBadRequest(string message)
        {
            return BadRequest(new ApiResponse(400, message));
        }

        protected ActionResult<ApiResponse> HandleNotFound(string message = "Requested resource not found")
        {
            return NotFound(new ApiResponse(404, message));
        }
    }
}
