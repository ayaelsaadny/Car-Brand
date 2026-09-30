using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Enums;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVehicleService _vehicleService;

        public BookingService(IUnitOfWork unitOfWork, IVehicleService vehicleService)
        {
            _unitOfWork = unitOfWork;
            _vehicleService = vehicleService;
        }

        private static bool IsVehicleEligible(Vehicle vehicle, CreateBookingRequestDto dto)
        {
            if (dto.NumberOfPassengers > vehicle.PassengerCapacity) return false;
            if (vehicle.LuggageCapacity > 0 && dto.NumberOfLuggage > vehicle.LuggageCapacity) return false;
            return true;
        }

        public async Task<BookingRequestDto> CreateBookingAsync(CreateBookingRequestDto dto, int? authenticatedUserId = null, CancellationToken cancellationToken = default)
        {
            // 1. Business Rule: Validate Dates
            if (dto.PickupDateTime < DateTime.UtcNow.AddMinutes(-5))
            {
                throw new InvalidOperationException("Pickup date and time must be in the future.");
            }

            if (dto.ReturnDateTime.HasValue && dto.ReturnDateTime.Value <= dto.PickupDateTime)
            {
                throw new InvalidOperationException("Return date and time must be after the pickup date and time.");
            }

            // 2. Validate Service & Category Availability
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(dto.ServiceId, cancellationToken);
            if (service == null || !service.IsActive)
            {
                throw new InvalidOperationException($"Service with ID {dto.ServiceId} is inactive and cannot accept new bookings.");
            }

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            if (category == null || !category.IsActive)
            {
                throw new InvalidOperationException($"The category for service '{service.Name}' is inactive and cannot accept new bookings.");
            }

            // 3. Resolve Vehicle Selection & Availability
            var activeVehicles = await _unitOfWork.Vehicles.GetAllWithImagesAsync(activeOnly: true, cancellationToken);
            if (!activeVehicles.Any())
            {
                throw new InvalidOperationException("No active vehicles are currently available in the fleet.");
            }

            Vehicle? vehicle;

            if (activeVehicles.Count == 1)
            {
                // Initial MVP: If there is exactly one active vehicle:
                // The active vehicle is automatically selected.
                // The customer does not need to manually select a vehicle.
                vehicle = activeVehicles[0];
            }
            else
            {
                // Multiple Vehicles:
                if (dto.VehicleId.HasValue && dto.VehicleId.Value > 0)
                {
                    // The customer can select a vehicle if vehicle selection is enabled for the service.
                    if (!service.AllowVehicleSelection)
                    {
                        throw new InvalidOperationException($"Vehicle selection is disabled for service '{service.Name}'. A vehicle will be automatically assigned.");
                    }

                    vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(dto.VehicleId.Value, cancellationToken);
                }
                else
                {
                    // Customer did not supply a VehicleId
                    if (!service.AllowVehicleSelection)
                    {
                        // Vehicle selection disabled: auto-assign default or first available eligible vehicle
                        vehicle = await _unitOfWork.Vehicles.GetDefaultVehicleAsync(cancellationToken);
                        if (vehicle == null || !IsVehicleEligible(vehicle, dto) || !await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, dto.PickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
                        {
                            vehicle = null;
                            foreach (var v in activeVehicles)
                            {
                                if (IsVehicleEligible(v, dto) && await _vehicleService.IsVehicleAvailableAsync(v.Id, dto.PickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
                                {
                                    vehicle = v;
                                    break;
                                }
                            }
                        }

                        if (vehicle == null)
                        {
                            throw new InvalidOperationException($"No suitable vehicle is available for service '{service.Name}' at the requested time slot.");
                        }
                    }
                    else
                    {
                        // Vehicle selection is enabled: try default vehicle if eligible & available, otherwise prompt to select
                        vehicle = await _unitOfWork.Vehicles.GetDefaultVehicleAsync(cancellationToken);
                        if (vehicle == null || !IsVehicleEligible(vehicle, dto) || !await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, dto.PickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
                        {
                            throw new InvalidOperationException("Multiple vehicles are available for this service. Please select a vehicle for your booking.");
                        }
                    }
                }
            }

            // 4. Strict Backend Validation: NEVER TRUST a vehicle ID without checking:
            // a) Exists
            if (vehicle == null)
            {
                throw new InvalidOperationException($"Vehicle with ID {dto.VehicleId} does not exist.");
            }

            // b) Is active
            if (!vehicle.IsActive)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' is currently inactive.");
            }

            // c) Is eligible for the requested booking
            if (dto.NumberOfPassengers > vehicle.PassengerCapacity)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' cannot accommodate {dto.NumberOfPassengers} passengers (maximum capacity: {vehicle.PassengerCapacity}).");
            }

            if (vehicle.LuggageCapacity > 0 && dto.NumberOfLuggage > vehicle.LuggageCapacity)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' cannot accommodate {dto.NumberOfLuggage} pieces of luggage (maximum capacity: {vehicle.LuggageCapacity}).");
            }

            // d) Is available according to the defined availability rules
            var isAvailable = await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, dto.PickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken);
            if (!isAvailable)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' is not available for the requested time slot. Please choose another vehicle or a different time.");
            }

            // 5. Resolve Customer Profile & Snapshot Information
            Customer customer;
            string snapshotFullName;
            string snapshotEmail;
            string snapshotPhoneNumber;

            if (authenticatedUserId.HasValue)
            {
                var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(authenticatedUserId.Value, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    throw new InvalidOperationException("Authenticated user account is inactive or not found.");
                }

                var existingCustomer = await _unitOfWork.Repository<Customer>()
                    .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

                if (existingCustomer == null)
                {
                    var names = (user.FullName ?? "Customer").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    customer = new Customer
                    {
                        UserId = user.Id,
                        FirstName = dto.Customer?.FirstName ?? (names.Length > 0 ? names[0] : "Customer"),
                        LastName = dto.Customer?.LastName ?? (names.Length > 1 ? names[1] : string.Empty),
                        Email = user.Email,
                        PhoneNumber = dto.Customer?.PhoneNumber ?? user.PhoneNumber ?? string.Empty,
                        Address = dto.Customer?.Address,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
                else
                {
                    customer = existingCustomer;
                }

                // Use latest saved profile information for future bookings
                snapshotFullName = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.FirstName)
                    ? $"{dto.Customer.FirstName} {dto.Customer.LastName}".Trim()
                    : customer.FullName;

                snapshotEmail = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.Email)
                    ? dto.Customer.Email.Trim().ToLower()
                    : customer.Email;

                snapshotPhoneNumber = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.PhoneNumber)
                    ? dto.Customer.PhoneNumber.Trim()
                    : customer.PhoneNumber;
            }
            else
            {
                if (dto.Customer == null)
                {
                    throw new InvalidOperationException("Customer details are required for unauthenticated bookings.");
                }

                snapshotFullName = $"{dto.Customer.FirstName} {dto.Customer.LastName}".Trim();
                snapshotEmail = dto.Customer.Email.Trim().ToLower();
                snapshotPhoneNumber = dto.Customer.PhoneNumber.Trim();

                var existingCustomer = await _unitOfWork.Repository<Customer>()
                    .FirstOrDefaultAsync(c => c.Email == snapshotEmail, cancellationToken);

                if (existingCustomer != null)
                {
                    customer = existingCustomer;
                }
                else
                {
                    customer = new Customer
                    {
                        FirstName = dto.Customer.FirstName.Trim(),
                        LastName = dto.Customer.LastName.Trim(),
                        Email = snapshotEmail,
                        PhoneNumber = snapshotPhoneNumber,
                        Address = dto.Customer.Address,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            // 6. Generate Unique Booking Reference
            var bookingReference = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            // 7. Calculate Estimated Price
            decimal? estimatedPrice = service.Price ?? vehicle.PricePerHour;

            // 8. Create Booking Request with Preserved Historical Customer Snapshot
            var booking = new BookingRequest
            {
                BookingReference = bookingReference,
                CustomerId = customer.Id,
                CustomerFullName = snapshotFullName,
                CustomerEmail = snapshotEmail,
                CustomerPhoneNumber = snapshotPhoneNumber,
                ServiceId = service.Id,
                VehicleId = vehicle.Id,
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation,
                PickupDateTime = dto.PickupDateTime,
                ReturnDateTime = dto.ReturnDateTime,
                FlightNumber = dto.FlightNumber,
                NumberOfPassengers = dto.NumberOfPassengers,
                NumberOfLuggage = dto.NumberOfLuggage,
                SpecialNotes = dto.SpecialNotes,
                EstimatedPrice = estimatedPrice,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.BookingRequests.AddAsync(booking, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // 9. Initial Booking Status History
            var initialHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = null,
                NewStatus = BookingStatus.Pending,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = customer.FullName.Length > 0 ? customer.FullName : "Customer",
                Notes = "Booking request submitted."
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(initialHistory, cancellationToken);

            // 10. Create System Notification
            var notification = new Notification
            {
                UserId = customer.UserId,
                Title = "Booking Request Submitted",
                Message = $"Booking {bookingReference} for {service.Name} was submitted successfully with status Pending.",
                Type = "Booking",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // 11. Fetch and return complete booking
            var createdBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(booking.Id, cancellationToken);
            return MapToDto(createdBooking ?? booking);
        }

        public async Task<BookingRequestDto?> GetBookingByIdAsync(int id, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            // Authorization check: non-admin can only see their own booking
            if (!isAdmin && authenticatedUserId.HasValue)
            {
                if (booking.Customer?.UserId != authenticatedUserId.Value)
                {
                    return null;
                }
            }

            return MapToDto(booking);
        }

        public async Task<BookingRequestDto?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        {
            var booking = await _unitOfWork.BookingRequests.GetByReferenceWithDetailsAsync(reference, cancellationToken);
            return booking == null ? null : MapToDto(booking);
        }

        public async Task<IReadOnlyList<BookingRequestDto>> GetUserBookingsAsync(int authenticatedUserId, CancellationToken cancellationToken = default)
        {
            var customer = await _unitOfWork.Repository<Customer>().FirstOrDefaultAsync(c => c.UserId == authenticatedUserId, cancellationToken);
            if (customer == null)
            {
                return new List<BookingRequestDto>();
            }

            var bookings = await _unitOfWork.BookingRequests.GetByCustomerIdWithDetailsAsync(customer.Id, cancellationToken);
            return bookings.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<BookingRequestDto>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
        {
            var bookings = await _unitOfWork.BookingRequests.GetAllWithDetailsAsync(cancellationToken);
            return bookings.Select(MapToDto).ToList();
        }

        public async Task<BookingRequestDto?> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto dto, string changedBy, CancellationToken cancellationToken = default)
        {
            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            var oldStatus = booking.Status;
            if (oldStatus == dto.Status)
            {
                return MapToDto(booking);
            }

            booking.Status = dto.Status;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BookingRequests.Update(booking);

            var history = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = oldStatus,
                NewStatus = dto.Status,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = dto.Notes
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(history, cancellationToken);

            var notification = new Notification
            {
                UserId = booking.Customer?.UserId,
                Title = $"Booking Status: {dto.Status}",
                Message = $"Booking {booking.BookingReference} status changed from {oldStatus} to {dto.Status}.",
                Type = "Booking",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var updatedBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            return MapToDto(updatedBooking ?? booking);
        }

        private static BookingRequestDto MapToDto(BookingRequest booking)
        {
            return new BookingRequestDto
            {
                Id = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerId = booking.CustomerId,
                Customer = new CustomerDto
                {
                    Id = booking.CustomerId,
                    FirstName = GetFirstName(booking.CustomerFullName, booking.Customer?.FirstName),
                    LastName = GetLastName(booking.CustomerFullName, booking.Customer?.LastName),
                    FullName = !string.IsNullOrWhiteSpace(booking.CustomerFullName) ? booking.CustomerFullName : (booking.Customer?.FullName ?? string.Empty),
                    Email = !string.IsNullOrWhiteSpace(booking.CustomerEmail) ? booking.CustomerEmail : (booking.Customer?.Email ?? string.Empty),
                    PhoneNumber = !string.IsNullOrWhiteSpace(booking.CustomerPhoneNumber) ? booking.CustomerPhoneNumber : (booking.Customer?.PhoneNumber ?? string.Empty),
                    Address = booking.Customer?.Address
                },
                ServiceId = booking.ServiceId,
                ServiceName = booking.Service?.Name ?? string.Empty,
                CategoryName = booking.Service?.Category?.Name ?? string.Empty,
                VehicleId = booking.VehicleId,
                VehicleName = booking.Vehicle != null ? $"{booking.Vehicle.Make} {booking.Vehicle.Model} ({booking.Vehicle.Name})" : string.Empty,
                VehicleLicensePlate = booking.Vehicle?.LicensePlate ?? string.Empty,
                VehicleImageUrl = booking.Vehicle?.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? booking.Vehicle?.Images?.FirstOrDefault()?.ImageUrl,
                PickupLocation = booking.PickupLocation,
                DropoffLocation = booking.DropoffLocation,
                PickupDateTime = booking.PickupDateTime,
                ReturnDateTime = booking.ReturnDateTime,
                FlightNumber = booking.FlightNumber,
                NumberOfPassengers = booking.NumberOfPassengers,
                NumberOfLuggage = booking.NumberOfLuggage,
                SpecialNotes = booking.SpecialNotes,
                EstimatedPrice = booking.EstimatedPrice,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                StatusHistories = booking.StatusHistories?.Select(h => new BookingStatusHistoryDto
                {
                    Id = h.Id,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    ChangedAt = h.ChangedAt,
                    ChangedBy = h.ChangedBy,
                    Notes = h.Notes
                }).ToList() ?? new List<BookingStatusHistoryDto>()
            };
        }

        private static string GetFirstName(string fullName, string? fallbackFirstName)
        {
            if (!string.IsNullOrWhiteSpace(fallbackFirstName)) return fallbackFirstName;
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fullName;
        }

        private static string GetLastName(string fullName, string? fallbackLastName)
        {
            if (!string.IsNullOrWhiteSpace(fallbackLastName)) return fallbackLastName;
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }
    }
}
