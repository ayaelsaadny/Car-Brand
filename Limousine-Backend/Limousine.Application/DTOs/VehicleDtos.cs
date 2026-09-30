using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class VehicleImageDto
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsMain => IsPrimary;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int PassengerCapacity { get; set; }
        public int LuggageCapacity { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal? PricePerKm { get; set; }
        public string? Features { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<VehicleImageDto> Images { get; set; } = new List<VehicleImageDto>();
    }

    public class CreateVehicleDto
    {
        [Required(ErrorMessage = "Vehicle name is required")]
        [StringLength(100, ErrorMessage = "Vehicle name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Make is required")]
        [StringLength(100, ErrorMessage = "Make cannot exceed 100 characters")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required")]
        [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100, ErrorMessage = "Please provide a valid year")]
        public int Year { get; set; }

        [Required(ErrorMessage = "License plate is required")]
        [StringLength(50, ErrorMessage = "License plate cannot exceed 50 characters")]
        public string LicensePlate { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
        public string? Color { get; set; }

        [Range(1, 100, ErrorMessage = "Passenger capacity must be at least 1")]
        public int PassengerCapacity { get; set; } = 4;

        [Range(0, 100, ErrorMessage = "Luggage capacity cannot be negative")]
        public int LuggageCapacity { get; set; } = 3;

        [Range(0.01, 100000, ErrorMessage = "Price per hour must be greater than zero")]
        public decimal PricePerHour { get; set; }

        [Range(0, 100000, ErrorMessage = "Price per km must be greater than or equal to zero")]
        public decimal? PricePerKm { get; set; }

        [StringLength(1000, ErrorMessage = "Features cannot exceed 1000 characters")]
        public string? Features { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        public List<CreateVehicleImageDto>? Images { get; set; }
    }

    public class UpdateVehicleDto
    {
        [Required(ErrorMessage = "Vehicle name is required")]
        [StringLength(100, ErrorMessage = "Vehicle name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Make is required")]
        [StringLength(100, ErrorMessage = "Make cannot exceed 100 characters")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required")]
        [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100, ErrorMessage = "Please provide a valid year")]
        public int Year { get; set; }

        [Required(ErrorMessage = "License plate is required")]
        [StringLength(50, ErrorMessage = "License plate cannot exceed 50 characters")]
        public string LicensePlate { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
        public string? Color { get; set; }

        [Range(1, 100, ErrorMessage = "Passenger capacity must be at least 1")]
        public int PassengerCapacity { get; set; } = 4;

        [Range(0, 100, ErrorMessage = "Luggage capacity cannot be negative")]
        public int LuggageCapacity { get; set; } = 3;

        [Range(0.01, 100000, ErrorMessage = "Price per hour must be greater than zero")]
        public decimal PricePerHour { get; set; }

        [Range(0, 100000, ErrorMessage = "Price per km must be greater than or equal to zero")]
        public decimal? PricePerKm { get; set; }

        [StringLength(1000, ErrorMessage = "Features cannot exceed 1000 characters")]
        public string? Features { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }

    public class CreateVehicleImageDto
    {
        [Required(ErrorMessage = "Image URL is required")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
        public bool IsMain
        {
            get => IsPrimary;
            set => IsPrimary = value;
        }
        public int DisplayOrder { get; set; } = 0;
    }

    public class VehicleImageOrderDto
    {
        [Required(ErrorMessage = "ImageId is required")]
        public int ImageId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative integer")]
        public int DisplayOrder { get; set; }
    }

    public class ReorderVehicleImagesDto
    {
        [Required(ErrorMessage = "Orders list is required")]
        public List<VehicleImageOrderDto> Orders { get; set; } = new();
    }

    public class UpdateVehicleStatusDto
    {
        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }

    public class UpdateVehicleDefaultDto
    {
        [Required(ErrorMessage = "IsDefault is required")]
        public bool IsDefault { get; set; } = true;
    }

    public class AvailableVehiclesQueryDto
    {
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Pickup date and time is required")]
        public DateTime PickupDateTime { get; set; }

        public DateTime? ReturnDateTime { get; set; }

        [Range(1, 100, ErrorMessage = "Number of passengers must be at least 1")]
        public int NumberOfPassengers { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Number of luggage cannot be negative")]
        public int NumberOfLuggage { get; set; } = 0;
    }

    public class AvailableVehicleDto
    {
        public VehicleDto Vehicle { get; set; } = null!;
        public bool IsEligible { get; set; }
        public bool IsAvailable { get; set; }
        public bool CanBeSelected { get; set; }
        public string? IneligibilityReason { get; set; }
        public string? UnavailabilityReason { get; set; }
    }

    public class DefaultVehicleDto : VehicleDto
    {
        public bool? IsAvailable { get; set; }
        public string? UnavailabilityReason { get; set; }
        public string AvailabilityNote => "Default status does not automatically imply permanent availability for every date/time.";
    }

    public class VehicleAvailabilityResponseDto
    {
        public bool AutoSelected { get; set; }
        public bool VehicleSelectionEnabled { get; set; } = true;
        public VehicleDto? DefaultVehicle { get; set; }
        public bool IsDefaultVehicleAvailable { get; set; }
        public VehicleDto? InitiallySelectedVehicle { get; set; }
        public VehicleDto? SelectedVehicle { get; set; }
        public IReadOnlyList<AvailableVehicleDto> AvailableVehicles { get; set; } = new List<AvailableVehicleDto>();
    }
}
