using System;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Image => ImageUrl;
        public decimal? Price { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public bool IsActive { get; set; }
        public bool AllowVehicleSelection { get; set; } = true;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalBookings { get; set; }
    }

    public class CreateServiceDto
    {
        [Required(ErrorMessage = "Service name is required")]
        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "ImageUrl cannot exceed 500 characters")]
        public string? ImageUrl { get; set; }

        [Range(0, 100000, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "CategoryId is required")]
        public int CategoryId { get; set; }

        [StringLength(250, ErrorMessage = "FromLocation cannot exceed 250 characters")]
        public string? FromLocation { get; set; }

        [StringLength(250, ErrorMessage = "ToLocation cannot exceed 250 characters")]
        public string? ToLocation { get; set; }

        public bool IsActive { get; set; } = true;
        public bool AllowVehicleSelection { get; set; } = true;

        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateServiceDto
    {
        [Required(ErrorMessage = "Service name is required")]
        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "ImageUrl cannot exceed 500 characters")]
        public string? ImageUrl { get; set; }

        [Range(0, 100000, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "CategoryId is required")]
        public int CategoryId { get; set; }

        [StringLength(250, ErrorMessage = "FromLocation cannot exceed 250 characters")]
        public string? FromLocation { get; set; }

        [StringLength(250, ErrorMessage = "ToLocation cannot exceed 250 characters")]
        public string? ToLocation { get; set; }

        public bool IsActive { get; set; } = true;
        public bool AllowVehicleSelection { get; set; } = true;

        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateServiceStatusDto
    {
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }

    public class UpdateServiceDisplayOrderDto
    {
        [Range(0, 100000, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; }
    }

    public class ServiceFilterQueryDto
    {
        public int? CategoryId { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? AllowVehicleSelection { get; set; }
        public string? SortBy { get; set; }
        public bool? ActiveOnly { get; set; }
    }
}
