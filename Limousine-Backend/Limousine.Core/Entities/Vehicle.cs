using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class Vehicle : BaseEntitiy
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int PassengerCapacity { get; set; } = 4;
        public int LuggageCapacity { get; set; } = 3;
        public decimal PricePerHour { get; set; }
        public decimal? PricePerKm { get; set; }
        public string? Features { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();
        public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
    }
}
