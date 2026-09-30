using System;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class WebsiteSettingDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateWebsiteSettingDto
    {
        [Required(ErrorMessage = "Value is required")]
        public string Value { get; set; } = string.Empty;
    }
}
