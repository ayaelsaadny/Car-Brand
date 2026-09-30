using Limousine.Core.Enums;
using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class ApplicationUser : BaseEntitiy
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Customer? Customer { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
