using Limousine.API.Errors;
using Limousine.API.Extensions;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Public - Reviews")]
    public class ReviewsController : BaseApiController
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Retrieves all approved customer reviews for public display.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReviewDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewDto>>>> GetApprovedReviews(CancellationToken cancellationToken)
        {
            var reviews = await _reviewService.GetApprovedReviewsAsync(cancellationToken);
            return HandleResult(reviews);
        }

        /// <summary>
        /// Submits a review. Can be called by registered users or guest users.
        /// The review is set to Pending until an Administrator reviews and approves it.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ReviewDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> SubmitReview([FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
            {
                return BadRequest(new ApiResponse(400, "Rating must be between 1 and 5 stars."));
            }

            int? userId = User?.GetUserId();
            string? fallbackName = User?.FindFirst(ClaimTypes.Name)?.Value 
                                   ?? User?.FindFirst("name")?.Value 
                                   ?? User?.GetUserEmail();

            var created = await _reviewService.CreateReviewAsync(dto, userId, fallbackName, cancellationToken);

            return HandleCreated(created, "Your review has been submitted successfully and will appear once approved by our team.");
        }
    }
}
