using System;
using System.Collections.Generic;

namespace Limousine.Core.Entities
{
    public class Category : BaseEntitiy
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<CategoryTranslation> Translations { get; set; } = new List<CategoryTranslation>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
