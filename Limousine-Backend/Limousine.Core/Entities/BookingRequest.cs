using Limousine.Core.Enums;
using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class BookingRequest : BaseEntitiy
    {
        public string BookingReference { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        /// <summary>
        /// Historical customer snapshot preserved at the moment of booking submission.
        /// Updating customer profile does not mutate these historical records.
        /// </summary>
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime PickupDateTime { get; set; }
        public DateTime? ReturnDateTime { get; set; }

        public string? FlightNumber { get; set; }
        public int NumberOfPassengers { get; set; } = 1;
        public int NumberOfLuggage { get; set; } = 0;
        public string? SpecialNotes { get; set; }

        public decimal? EstimatedPrice { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<BookingStatusHistory> StatusHistories { get; set; } = new List<BookingStatusHistory>();
    }
}
