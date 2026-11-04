using Limousine.API.Errors;
using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reviews")]
    [Tags("Admin - Reviews")]
    public class AdminReviewsController : BaseApiController
    {
        private readonly IReviewService _reviewService;

        public AdminReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Retrieves all reviews for moderation (can filter by isApproved: null = all, true = approved, false = pending/rejected).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReviewDto>>), 200)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewDto>>>> GetAllReviews([FromQuery] bool? isApproved, CancellationToken cancellationToken)
        {
            var reviews = await _reviewService.GetAllReviewsAsync(isApproved, cancellationToken);
            return HandleResult(reviews);
        }

        /// <summary>
        /// Approves a review so it appears publicly on the website.
        /// </summary>
        [HttpPut("{id:int}/approve")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> ApproveReview(int id, CancellationToken cancellationToken)
        {
            var success = await _reviewService.UpdateReviewStatusAsync(id, true, cancellationToken);
            if (!success)
            {
                return HandleNotFound("Review not found.");
            }

            return HandleSuccess("Review approved successfully and published to public site.");
        }

        /// <summary>
        /// Rejects a review so it remains hidden from the public website.
        /// </summary>
        [HttpPut("{id:int}/reject")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> RejectReview(int id, CancellationToken cancellationToken)
        {
            var success = await _reviewService.UpdateReviewStatusAsync(id, false, cancellationToken);
            if (!success)
            {
                return HandleNotFound("Review not found.");
            }

            return HandleSuccess("Review status updated to rejected.");
        }

        /// <summary>
        /// Permanently deletes a review.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> DeleteReview(int id, CancellationToken cancellationToken)
        {
            var success = await _reviewService.DeleteReviewAsync(id, cancellationToken);
            if (!success)
            {
                return HandleNotFound("Review not found.");
            }

            return HandleSuccess("Review deleted successfully.");
        }
    }
}
