using Limousine.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class BookingCustomerDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class CreateBookingRequestDto
    {
        [Required(ErrorMessage = "Service is required")]
        public int ServiceId { get; set; }

        /// <summary>
        /// Optional: If not provided, system automatically assigns the active default vehicle.
        /// This ensures the booking works with 1 vehicle or multiple vehicles seamlessly.
        /// </summary>
        public int? VehicleId { get; set; }

        [Required(ErrorMessage = "Pickup location is required")]
        [StringLength(300, ErrorMessage = "Pickup location cannot exceed 300 characters")]
        public string PickupLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dropoff location is required")]
        [StringLength(300, ErrorMessage = "Dropoff location cannot exceed 300 characters")]
        public string DropoffLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pickup date and time is required")]
        public DateTime PickupDateTime { get; set; }

        public DateTime? ReturnDateTime { get; set; }

        [StringLength(50, ErrorMessage = "Flight number cannot exceed 50 characters")]
        public string? FlightNumber { get; set; }

        [Range(1, 100, ErrorMessage = "Number of passengers must be at least 1")]
        public int NumberOfPassengers { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Number of luggage cannot be negative")]
        public int NumberOfLuggage { get; set; } = 0;

        [StringLength(1000, ErrorMessage = "Special notes cannot exceed 1000 characters")]
        public string? SpecialNotes { get; set; }

        /// <summary>
        /// Customer details. Can be omitted if customer is authenticated.
        /// </summary>
        public BookingCustomerDto? Customer { get; set; }
    }

    public class BookingStatusHistoryDto
    {
        public int Id { get; set; }
        public BookingStatus? PreviousStatus { get; set; }
        public BookingStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class BookingRequestDto
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public CustomerDto Customer { get; set; } = null!;

        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public string? VehicleImageUrl { get; set; }

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime PickupDateTime { get; set; }
        public DateTime? ReturnDateTime { get; set; }

        public string? FlightNumber { get; set; }
        public int NumberOfPassengers { get; set; }
        public int NumberOfLuggage { get; set; }
        public string? SpecialNotes { get; set; }

        public decimal? EstimatedPrice { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusName => Status.ToString();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<BookingStatusHistoryDto> StatusHistories { get; set; } = new List<BookingStatusHistoryDto>();
    }

    public class UpdateBookingStatusDto
    {
        [Required(ErrorMessage = "New status is required")]
        public BookingStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
}
