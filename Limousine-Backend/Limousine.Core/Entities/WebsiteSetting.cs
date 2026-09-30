using System;

namespace Limousine.Core.Entities
{
    public class WebsiteSetting : BaseEntitiy
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; } = "General";
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
