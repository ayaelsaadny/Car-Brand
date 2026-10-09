using System;

namespace Limousine.Core.Entities
{
    public class ServiceTranslation : BaseEntitiy
    {
        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public string LanguageCode { get; set; } = string.Empty; // "en" or "fr"
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
