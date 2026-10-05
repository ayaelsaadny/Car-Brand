using Limousine.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Limousine.Application.DTOs
{
    public class BookingCustomerDto : IValidatableObject
    {
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? LastName { get; set; }

        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
        public string? Address { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FullName) && string.IsNullOrWhiteSpace(FirstName))
            {
                yield return new ValidationResult("Either full name or first name is required.", new[] { nameof(FullName), nameof(FirstName) });
            }
        }
    }

    public class GuestBookingRequestDto
    {
        /// <summary>
        /// CustomerId supplied by the frontend (if any) is ignored. Guest bookings always have CustomerId = null.
        /// </summary>
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; } = string.Empty;

        public string? CustomerName
        {
            get => FullName;
            set { if (!string.IsNullOrWhiteSpace(value)) FullName = value; }
        }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = string.Empty;

        public string? CustomerEmail
        {
            get => Email;
            set { if (!string.IsNullOrWhiteSpace(value)) Email = value; }
        }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? CustomerPhone
        {
            get => PhoneNumber;
            set { if (!string.IsNullOrWhiteSpace(value)) PhoneNumber = value; }
        }

        [Required(ErrorMessage = "Service is required")]
        public int ServiceId { get; set; }

        /// <summary>
        /// Optional: If not provided, system automatically assigns the active default vehicle.
        /// </summary>
        public int? VehicleId { get; set; }

        [Required(ErrorMessage = "Pickup location is required")]
        [StringLength(300, ErrorMessage = "Pickup location cannot exceed 300 characters")]
        public string PickupLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dropoff location is required")]
        [StringLength(300, ErrorMessage = "Dropoff location cannot exceed 300 characters")]
        public string DropoffLocation { get; set; } = string.Empty;

        public DateTime? PickupDateTime { get; set; }

        public string? PickupDate { get; set; }

        public string? PickupTime { get; set; }

        public DateTime? ReturnDateTime { get; set; }

        [StringLength(50, ErrorMessage = "Flight number cannot exceed 50 characters")]
        public string? FlightNumber { get; set; }

        [Range(1, 100, ErrorMessage = "Number of passengers must be at least 1")]
        public int NumberOfPassengers { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Number of luggage cannot be negative")]
        public int NumberOfLuggage { get; set; } = 0;

        [StringLength(1000, ErrorMessage = "Special notes cannot exceed 1000 characters")]
        public string? SpecialNotes { get; set; }

        [StringLength(1000, ErrorMessage = "Customer notes cannot exceed 1000 characters")]
        public string? CustomerNotes
        {
            get => SpecialNotes;
            set { if (value != null) SpecialNotes = value; }
        }
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

    public class BookingFormProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class BookingFormInitDto
    {
        public bool IsAuthenticated { get; set; }
        public BookingFormProfileDto? Profile { get; set; }
        public ServiceDto? SelectedService { get; set; }
    }

    public class CreateBookingRequestDto
    {
        /// <summary>
        /// CustomerId supplied by the frontend (if any) is NEVER trusted by the backend.
        /// Authenticated requests obtain CustomerId from user claims; unauthenticated requests result in CustomerId = null.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Arbitrary status cannot be assigned on booking creation.
        /// Every new booking must strictly be created with status Pending.
        /// </summary>
        public BookingStatus? Status { get; set; }

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

        public DateTime? PickupDateTime { get; set; }

        public string? PickupDate { get; set; }

        public string? PickupTime { get; set; }

        public DateTime? ReturnDateTime { get; set; }

        [StringLength(50, ErrorMessage = "Flight number cannot exceed 50 characters")]
        public string? FlightNumber { get; set; }

        [Range(1, 100, ErrorMessage = "Number of passengers must be at least 1")]
        public int NumberOfPassengers { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Number of luggage cannot be negative")]
        public int NumberOfLuggage { get; set; } = 0;

        [StringLength(1000, ErrorMessage = "Special notes cannot exceed 1000 characters")]
        public string? SpecialNotes { get; set; }

        [StringLength(1000, ErrorMessage = "Customer notes cannot exceed 1000 characters")]
        public string? CustomerNotes
        {
            get => SpecialNotes;
            set { if (value != null) SpecialNotes = value; }
        }

        /// <summary>
        /// Customer details. Can be omitted if customer is authenticated.
        /// </summary>
        public BookingCustomerDto? Customer { get; set; }

        public string? CustomerName
        {
            get => Customer?.FullName;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Customer ??= new BookingCustomerDto();
                    Customer.FullName = value;
                }
            }
        }

        public string? CustomerEmail
        {
            get => Customer?.Email;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Customer ??= new BookingCustomerDto();
                    Customer.Email = value;
                }
            }
        }

        public string? CustomerPhone
        {
            get => Customer?.PhoneNumber;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Customer ??= new BookingCustomerDto();
                    Customer.PhoneNumber = value;
                }
            }
        }
    }

    public class BookingStatusHistoryDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int BookingRequestId
        {
            get => BookingId;
            set => BookingId = value;
        }

        public BookingStatus? OldStatus { get; set; }
        public BookingStatus? PreviousStatus
        {
            get => OldStatus;
            set => OldStatus = value;
        }

        public BookingStatus NewStatus { get; set; }
        public string? ChangedBy { get; set; }

        public string? Reason { get; set; }
        public string? Notes
        {
            get => Reason;
            set => Reason = value;
        }

        public DateTime CreatedAt { get; set; }
        public DateTime ChangedAt
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }
    }

    public class BookingRequestDto
    {
        public int Id { get; set; }
        public string BookingReference { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceNameSnapshot { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleNameSnapshot { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public string? VehicleImageUrl { get; set; }

        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public DateTime PickupDateTime { get; set; }
        public string PickupDate { get; set; } = string.Empty;
        public string PickupTime { get; set; } = string.Empty;
        public DateTime? ReturnDateTime { get; set; }

        public string? FlightNumber { get; set; }
        public int NumberOfPassengers { get; set; }
        public int NumberOfLuggage { get; set; }
        public string? SpecialNotes { get; set; }
        public string? CustomerNotes { get; set; }

        public decimal? EstimatedPrice { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusName => Status.ToString();

        public string? RejectionReason { get; set; }
        public string? CancellationReason { get; set; }

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

    public class AcceptBookingDto
    {
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    public class RejectBookingDto
    {
        /// <summary>
        /// Optional rejection reason explaining why the pending request was rejected.
        /// </summary>
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
        public string? RejectionReason { get; set; }

        public string? Reason
        {
            get => RejectionReason;
            set { if (value != null) RejectionReason = value; }
        }
    }

    public class CancelBookingDto
    {
        /// <summary>
        /// Optional cancellation reason explaining why the booking was cancelled.
        /// </summary>
        [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
        public string? CancellationReason { get; set; }

        public string? Reason
        {
            get => CancellationReason;
            set { if (value != null) CancellationReason = value; }
        }
    }

    public class CompleteBookingDto
    {
        /// <summary>
        /// Optional notes for marking the booking as completed.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult() { }

        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }

    public class BookingSearchFilterDto
    {
        // Search fields
        public string? Search { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public int? BookingId { get; set; }
        public string? BookingReference { get; set; }

        // Filters
        public BookingStatus? Status { get; set; }
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public int? VehicleId { get; set; }
        public string? VehicleName { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Sorting
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;

        // Pagination
        private int _pageNumber = 1;
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
        }
    }
}
