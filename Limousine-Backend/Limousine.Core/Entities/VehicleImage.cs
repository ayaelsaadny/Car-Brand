using System;

namespace Limousine.Core.Entities
{
    public class VehicleImage : BaseEntitiy
    {
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public bool IsMain
        {
            get => IsPrimary;
            set => IsPrimary = value;
        }
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
