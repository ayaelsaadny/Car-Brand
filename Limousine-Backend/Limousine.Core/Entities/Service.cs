using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class Service : BaseEntitiy
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AllowVehicleSelection { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
    }
}
