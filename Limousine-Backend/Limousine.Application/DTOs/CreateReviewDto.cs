using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class CreateReviewDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        // Optional or provided for guest users
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string? City { get; set; }
    }
}
