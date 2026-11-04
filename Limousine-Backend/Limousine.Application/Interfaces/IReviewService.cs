using Limousine.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Interfaces
{
    public interface IReviewService
    {
        Task<IReadOnlyList<ReviewDto>> GetApprovedReviewsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReviewDto>> GetAllReviewsAsync(bool? isApproved = null, CancellationToken cancellationToken = default);
        Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, int? userId = null, string? fallbackName = null, CancellationToken cancellationToken = default);
        Task<bool> UpdateReviewStatusAsync(int id, bool isApproved, CancellationToken cancellationToken = default);
        Task<bool> DeleteReviewAsync(int id, CancellationToken cancellationToken = default);
    }
}
