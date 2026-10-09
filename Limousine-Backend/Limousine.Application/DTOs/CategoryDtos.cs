using System;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Image => ImageUrl;
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ServicesCount { get; set; }
        public List<ServiceDto>? Services { get; set; }
    }

    public class CreateCategoryDto
    {
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public Dictionary<string, EntityTranslationDto>? Translations { get; set; }

        [Required(ErrorMessage = "Category image is required")]
        [StringLength(500, ErrorMessage = "ImageUrl cannot exceed 500 characters")]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public Dictionary<string, EntityTranslationDto>? Translations { get; set; }

        [Required(ErrorMessage = "Category image is required")]
        [StringLength(500, ErrorMessage = "ImageUrl cannot exceed 500 characters")]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryStatusDto
    {
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }

    public class UpdateCategoryDisplayOrderDto
    {
        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; }
    }
}
