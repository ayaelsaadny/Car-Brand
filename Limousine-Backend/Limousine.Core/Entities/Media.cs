using System;

namespace Limousine.Core.Entities
{
    public class Media : BaseEntitiy
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? Title { get; set; }
        public string? AltText { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
