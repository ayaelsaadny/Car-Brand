using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class Customer : BaseEntitiy
    {
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
