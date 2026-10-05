using Limousine.Core.Enums;
using System;

namespace Limousine.Core.Entities
{
    public class BookingStatusHistory : BaseEntitiy
    {
        public int BookingRequestId { get; set; }
        public BookingRequest BookingRequest { get; set; } = null!;

        public int BookingId
        {
            get => BookingRequestId;
            set => BookingRequestId = value;
        }

        public BookingStatus? PreviousStatus { get; set; }
        public BookingStatus? OldStatus
        {
            get => PreviousStatus;
            set => PreviousStatus = value;
        }

        public BookingStatus NewStatus { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => ChangedAt;
            set => ChangedAt = value;
        }

        public string? ChangedBy { get; set; }

        public string? Notes { get; set; }
        public string? Reason
        {
            get => Notes;
            set => Notes = value;
        }
    }
}
