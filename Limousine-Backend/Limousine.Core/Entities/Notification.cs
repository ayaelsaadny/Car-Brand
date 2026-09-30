using System;

namespace Limousine.Core.Entities
{
    public class Notification : BaseEntitiy
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "General";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
