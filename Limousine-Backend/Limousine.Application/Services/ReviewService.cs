using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<ReviewDto>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default)
        {
            var reviews = await _unitOfWork.Repository<Review>()
                .FindAsync(r => r.IsApproved, cancellationToken);

            return reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapToDto)
                .ToList();
        }

        public async Task<IReadOnlyList<ReviewDto>> GetAllReviewsAsync(bool? isApproved = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Review> reviews;
            if (isApproved.HasValue)
            {
                reviews = await _unitOfWork.Repository<Review>()
                    .FindAsync(r => r.IsApproved == isApproved.Value, cancellationToken);
            }
            else
            {
                reviews = await _unitOfWork.Repository<Review>()
                    .GetAllAsync(cancellationToken);
            }

            return reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapToDto)
                .ToList();
        }

        public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, int? userId = null, string? fallbackName = null, CancellationToken cancellationToken = default)
        {
            string authorName = !string.IsNullOrWhiteSpace(dto.AuthorName)
                ? dto.AuthorName.Trim()
                : (!string.IsNullOrWhiteSpace(fallbackName) ? fallbackName.Trim() : "VIP Guest");

            var review = new Review
            {
                UserId = userId,
                AuthorName = authorName,
                AuthorEmail = dto.AuthorEmail?.Trim(),
                City = dto.City?.Trim(),
                Rating = Math.Clamp(dto.Rating, 1, 5),
                Comment = !string.IsNullOrWhiteSpace(dto.Comment) ? dto.Comment.Trim() : null,
                IsApproved = false, // Always requires Admin moderation first
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Review>().AddAsync(review, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return MapToDto(review);
        }

        public async Task<bool> UpdateReviewStatusAsync(int id, bool isApproved, CancellationToken cancellationToken = default)
        {
            var review = await _unitOfWork.Repository<Review>().GetByIdAsync(id, cancellationToken);
            if (review == null)
                return false;

            review.IsApproved = isApproved;
            review.ReviewedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Review>().Update(review);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteReviewAsync(int id, CancellationToken cancellationToken = default)
        {
            var review = await _unitOfWork.Repository<Review>().GetByIdAsync(id, cancellationToken);
            if (review == null)
                return false;

            _unitOfWork.Repository<Review>().Delete(review);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return true;
        }

        private static ReviewDto MapToDto(Review r)
        {
            return new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                AuthorName = r.AuthorName,
                AuthorEmail = r.AuthorEmail,
                City = r.City,
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt
            };
        }
    }
}
