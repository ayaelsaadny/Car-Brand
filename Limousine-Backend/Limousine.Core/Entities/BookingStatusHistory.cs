using Limousine.Core.Enums;
using System;

namespace Limousine.Core.Entities
{
    public class BookingStatusHistory : BaseEntitiy
    {
        public int BookingRequestId { get; set; }
        public BookingRequest BookingRequest { get; set; } = null!;

        public BookingStatus? PreviousStatus { get; set; }
        public BookingStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? ChangedBy { get; set; }
        public string? Notes { get; set; }
    }
}
