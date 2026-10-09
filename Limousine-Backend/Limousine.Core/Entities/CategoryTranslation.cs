using System;

namespace Limousine.Core.Entities
{
    public class CategoryTranslation : BaseEntitiy
    {
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string LanguageCode { get; set; } = string.Empty; // "en" or "fr"
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
