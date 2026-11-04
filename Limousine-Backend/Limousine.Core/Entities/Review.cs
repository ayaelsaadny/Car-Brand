using System;

namespace Limousine.Core.Entities
{
    public class Review : BaseEntitiy
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string? City { get; set; }

        public int Rating { get; set; } // 1 to 5 stars
        public string? Comment { get; set; } // Optional text comment

        public bool IsApproved { get; set; } = false; // Requires Admin approval
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
