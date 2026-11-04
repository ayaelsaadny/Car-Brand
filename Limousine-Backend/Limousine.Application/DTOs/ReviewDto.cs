using System;

namespace Limousine.Application.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string? City { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IsGuest => !UserId.HasValue;
    }
}
