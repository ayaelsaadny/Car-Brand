using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Enums;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static void ValidateCustomerInformation(string fullName, string email, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new InvalidOperationException("Full name is required.");
            }

            if (fullName.Trim().Length > 150)
            {
                throw new InvalidOperationException("Full name cannot exceed 150 characters.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required.");
            }

            if (email.Trim().Length > 150)
            {
                throw new InvalidOperationException("Email cannot exceed 150 characters.");
            }

            if (!email.Contains('@'))
            {
                throw new InvalidOperationException("Please provide a valid email address.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new InvalidOperationException("Phone number is required.");
            }
        }

        private static bool IsValidStatusTransition(BookingStatus currentStatus, BookingStatus newStatus)
        {
            if (currentStatus == newStatus) return true;

            return currentStatus switch
            {
                BookingStatus.Pending => newStatus is BookingStatus.Accepted or BookingStatus.Confirmed or BookingStatus.Rejected or BookingStatus.Cancelled,
                BookingStatus.Accepted or BookingStatus.Confirmed => newStatus is BookingStatus.Completed or BookingStatus.InProgress or BookingStatus.Cancelled,
                BookingStatus.InProgress => newStatus is BookingStatus.Completed or BookingStatus.Cancelled,
                _ => false // Terminal states (Completed, Rejected, Cancelled) have no further allowed transitions
            };
        }

        private static string GetAllowedTransitionsDescription(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.Pending => "Accepted, Rejected, Cancelled",
                BookingStatus.Accepted or BookingStatus.Confirmed => "Completed, InProgress, Cancelled",
                BookingStatus.InProgress => "Completed, Cancelled",
                _ => "None (terminal state)"
            };
        }

        public async Task<BookingRequestDto> CreateBookingAsync(CreateBookingRequestDto dto, int? authenticatedUserId = null, CancellationToken cancellationToken = default)
        {
            // Business Rule: Frontend must not be able to directly assign an arbitrary status.
            // New bookings must always be created as Pending.
            if (dto.Status.HasValue && dto.Status.Value != BookingStatus.Pending)
            {
                throw new InvalidOperationException("New bookings must always be created with status 'Pending'. The client is not allowed to assign an arbitrary initial status.");
            }

            // Validate Client IDs
            if (dto.ServiceId <= 0)
            {
                throw new InvalidOperationException("A valid Service ID is required.");
            }

            if (dto.VehicleId.HasValue && dto.VehicleId.Value <= 0)
            {
                throw new InvalidOperationException("Vehicle ID must be a positive integer.");
            }

            // Resolve effective pickup date and time from either PickupDateTime or (PickupDate + PickupTime)
            DateTime effectivePickupDateTime;
            if (dto.PickupDateTime.HasValue)
            {
                effectivePickupDateTime = dto.PickupDateTime.Value;
            }
            else if (!string.IsNullOrWhiteSpace(dto.PickupDate) && !string.IsNullOrWhiteSpace(dto.PickupTime))
            {
                if (!DateTime.TryParse($"{dto.PickupDate.Trim()} {dto.PickupTime.Trim()}", out effectivePickupDateTime))
                {
                    throw new InvalidOperationException("Invalid pickup date or time format. Please provide a valid date and time.");
                }
            }
            else
            {
                throw new InvalidOperationException("Pickup date and time is required.");
            }

            // 1. Business Rule: Validate Dates
            if (effectivePickupDateTime < DateTime.UtcNow.AddMinutes(-5))
            {
                throw new InvalidOperationException("Pickup date and time must be in the future.");
            }

            if (dto.ReturnDateTime.HasValue && dto.ReturnDateTime.Value <= effectivePickupDateTime)
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
                        if (vehicle == null || !IsVehicleEligible(vehicle, dto) || !await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, effectivePickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
                        {
                            vehicle = null;
                            foreach (var v in activeVehicles)
                            {
                                if (IsVehicleEligible(v, dto) && await _vehicleService.IsVehicleAvailableAsync(v.Id, effectivePickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
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
                        if (vehicle == null || !IsVehicleEligible(vehicle, dto) || !await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, effectivePickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken))
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
            var isAvailable = await _vehicleService.IsVehicleAvailableAsync(vehicle.Id, effectivePickupDateTime, dto.ReturnDateTime, cancellationToken: cancellationToken);
            if (!isAvailable)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' is not available for the requested time slot. Please choose another vehicle or a different time.");
            }

            // 5. Resolve Customer Profile & Snapshot Information
            Customer? customer = null;
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

                // Auto-populate customer snapshot information from saved profile, or use modified values if customer modified them on the form
                snapshotFullName = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.FullName)
                    ? dto.Customer.FullName.Trim()
                    : (dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.FirstName)
                        ? $"{dto.Customer.FirstName} {dto.Customer.LastName}".Trim()
                        : (!string.IsNullOrWhiteSpace(customer.FullName) ? customer.FullName : (user.FullName ?? string.Empty)));

                snapshotEmail = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.Email)
                    ? dto.Customer.Email.Trim().ToLower()
                    : (!string.IsNullOrWhiteSpace(customer.Email) ? customer.Email.Trim().ToLower() : user.Email.Trim().ToLower());

                snapshotPhoneNumber = dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.PhoneNumber)
                    ? dto.Customer.PhoneNumber.Trim()
                    : (!string.IsNullOrWhiteSpace(customer.PhoneNumber) ? customer.PhoneNumber : (user.PhoneNumber ?? string.Empty));

                // If customer profile was missing phone number or address, save the newly provided info for future bookings
                var profileNeedsUpdate = false;
                if (string.IsNullOrWhiteSpace(customer.PhoneNumber) && !string.IsNullOrWhiteSpace(snapshotPhoneNumber))
                {
                    customer.PhoneNumber = snapshotPhoneNumber;
                    profileNeedsUpdate = true;
                }
                if (string.IsNullOrWhiteSpace(customer.Address) && dto.Customer != null && !string.IsNullOrWhiteSpace(dto.Customer.Address))
                {
                    customer.Address = dto.Customer.Address.Trim();
                    profileNeedsUpdate = true;
                }
                if (profileNeedsUpdate)
                {
                    _unitOfWork.Repository<Customer>().Update(customer);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }
            else
            {
                // Guest Customer flow:
                // CustomerId is null because guest customers do not have an account.
                snapshotFullName = !string.IsNullOrWhiteSpace(dto.Customer?.FullName)
                    ? dto.Customer.FullName.Trim()
                    : (!string.IsNullOrWhiteSpace(dto.CustomerName)
                        ? dto.CustomerName.Trim()
                        : (!string.IsNullOrWhiteSpace(dto.FullName)
                            ? dto.FullName.Trim()
                            : (!string.IsNullOrWhiteSpace(dto.Customer?.FirstName)
                                ? $"{dto.Customer.FirstName} {dto.Customer.LastName}".Trim()
                                : "Guest Customer")));

                if (string.IsNullOrWhiteSpace(snapshotFullName))
                {
                    snapshotFullName = "Guest Customer";
                }

                snapshotEmail = (!string.IsNullOrWhiteSpace(dto.Customer?.Email)
                    ? dto.Customer.Email
                    : (!string.IsNullOrWhiteSpace(dto.CustomerEmail)
                        ? dto.CustomerEmail
                        : (!string.IsNullOrWhiteSpace(dto.Email)
                            ? dto.Email
                            : "guest@limousine.com")))?.Trim().ToLower() ?? "guest@limousine.com";

                if (string.IsNullOrWhiteSpace(snapshotEmail) || !snapshotEmail.Contains('@'))
                {
                    snapshotEmail = "guest@limousine.com";
                }

                snapshotPhoneNumber = (!string.IsNullOrWhiteSpace(dto.Customer?.PhoneNumber)
                    ? dto.Customer.PhoneNumber
                    : (!string.IsNullOrWhiteSpace(dto.CustomerPhone)
                        ? dto.CustomerPhone
                        : (!string.IsNullOrWhiteSpace(dto.PhoneNumber)
                            ? dto.PhoneNumber
                            : (!string.IsNullOrWhiteSpace(dto.Phone)
                                ? dto.Phone
                                : "+1000000000"))))?.Trim() ?? "+1000000000";

                if (string.IsNullOrWhiteSpace(snapshotPhoneNumber))
                {
                    snapshotPhoneNumber = "+1000000000";
                }

                customer = null;
            }

            // 6. Strict Backend Validation: Validate all submitted customer information
            // regardless of whether it was automatically loaded from the profile or manually entered/modified.
            ValidateCustomerInformation(snapshotFullName, snapshotEmail, snapshotPhoneNumber);

            if (string.IsNullOrWhiteSpace(dto.PickupLocation))
            {
                throw new InvalidOperationException("Pickup location is required.");
            }

            if (dto.PickupLocation.Length > 300)
            {
                throw new InvalidOperationException("Pickup location cannot exceed 300 characters.");
            }

            if (string.IsNullOrWhiteSpace(dto.DropoffLocation))
            {
                throw new InvalidOperationException("Dropoff location is required.");
            }

            if (dto.DropoffLocation.Length > 300)
            {
                throw new InvalidOperationException("Dropoff location cannot exceed 300 characters.");
            }

            if (dto.NumberOfPassengers < 1)
            {
                throw new InvalidOperationException("Number of passengers must be at least 1.");
            }

            if (dto.NumberOfLuggage < 0)
            {
                throw new InvalidOperationException("Number of luggage cannot be negative.");
            }

            // 7. Generate Unique Booking Reference
            var bookingReference = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            // Calculate Estimated Price
            decimal? estimatedPrice = service.Price ?? vehicle.PricePerHour;

            // 8. Create Booking Request with Preserved Historical Customer and Service/Vehicle Snapshots
            var booking = new BookingRequest
            {
                BookingReference = bookingReference,
                CustomerId = customer?.Id, // Nullable for Guest bookings because guest customers do not have an account
                CustomerFullName = snapshotFullName,
                CustomerEmail = snapshotEmail,
                CustomerPhoneNumber = snapshotPhoneNumber,
                ServiceId = service.Id,
                ServiceNameSnapshot = service.Name,
                VehicleId = vehicle.Id,
                VehicleNameSnapshot = !string.IsNullOrWhiteSpace(vehicle.Name) ? vehicle.Name : $"{vehicle.Make} {vehicle.Model}".Trim(),
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation,
                PickupDateTime = effectivePickupDateTime,
                ReturnDateTime = dto.ReturnDateTime,
                FlightNumber = dto.FlightNumber,
                NumberOfPassengers = dto.NumberOfPassengers,
                NumberOfLuggage = dto.NumberOfLuggage,
                SpecialNotes = dto.CustomerNotes ?? dto.SpecialNotes,
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
                ChangedBy = !string.IsNullOrWhiteSpace(snapshotFullName) ? snapshotFullName : "Customer",
                Notes = authenticatedUserId.HasValue ? "Booking request submitted." : "Guest booking request submitted."
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(initialHistory, cancellationToken);

            // 10. Create System Notification for registered users
            if (customer?.UserId != null)
            {
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
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            // 11. Fetch and return complete booking
            var createdBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(booking.Id, cancellationToken);
            return MapToDto(createdBooking ?? booking);
        }

        public async Task<BookingRequestDto> CreateGuestBookingAsync(GuestBookingRequestDto dto, CancellationToken cancellationToken = default)
        {
            var createDto = new CreateBookingRequestDto
            {
                ServiceId = dto.ServiceId,
                VehicleId = dto.VehicleId,
                PickupLocation = dto.PickupLocation,
                DropoffLocation = dto.DropoffLocation,
                PickupDateTime = dto.PickupDateTime,
                PickupDate = dto.PickupDate,
                PickupTime = dto.PickupTime,
                ReturnDateTime = dto.ReturnDateTime,
                FlightNumber = dto.FlightNumber,
                NumberOfPassengers = dto.NumberOfPassengers,
                NumberOfLuggage = dto.NumberOfLuggage,
                SpecialNotes = dto.CustomerNotes ?? dto.SpecialNotes,
                Customer = new BookingCustomerDto
                {
                    FullName = !string.IsNullOrWhiteSpace(dto.CustomerName) ? dto.CustomerName : dto.FullName,
                    Email = !string.IsNullOrWhiteSpace(dto.CustomerEmail) ? dto.CustomerEmail : dto.Email,
                    PhoneNumber = !string.IsNullOrWhiteSpace(dto.CustomerPhone) ? dto.CustomerPhone : dto.PhoneNumber
                }
            };

            return await CreateBookingAsync(createDto, authenticatedUserId: null, cancellationToken: cancellationToken);
        }

        public async Task<BookingFormProfileDto?> GetBookingFormProfileAsync(int authenticatedUserId, CancellationToken cancellationToken = default)
        {
            if (authenticatedUserId <= 0)
            {
                throw new InvalidOperationException("A valid user ID is required.");
            }

            var user = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(authenticatedUserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            var customer = await _unitOfWork.Repository<Customer>()
                .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

            var userNames = (user.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = customer != null && !string.IsNullOrWhiteSpace(customer.FirstName)
                ? customer.FirstName
                : (userNames.Length > 0 ? userNames[0] : string.Empty);
            var lastName = customer != null && !string.IsNullOrWhiteSpace(customer.LastName)
                ? customer.LastName
                : (userNames.Length > 1 ? userNames[1] : string.Empty);

            var fullName = customer != null && !string.IsNullOrWhiteSpace(customer.FullName)
                ? customer.FullName
                : (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : $"{firstName} {lastName}".Trim());

            var email = customer != null && !string.IsNullOrWhiteSpace(customer.Email)
                ? customer.Email
                : user.Email;

            var phoneNumber = customer != null && !string.IsNullOrWhiteSpace(customer.PhoneNumber)
                ? customer.PhoneNumber
                : (user.PhoneNumber ?? string.Empty);

            return new BookingFormProfileDto
            {
                FullName = fullName,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = customer?.Address
            };
        }

        public async Task<BookingFormInitDto> GetBookingFormInitAsync(int? authenticatedUserId, int? serviceId = null, CancellationToken cancellationToken = default)
        {
            if (authenticatedUserId.HasValue && authenticatedUserId.Value <= 0)
            {
                throw new InvalidOperationException("User ID must be a positive integer.");
            }

            if (serviceId.HasValue && serviceId.Value <= 0)
            {
                throw new InvalidOperationException("Service ID must be a positive integer.");
            }

            BookingFormProfileDto? profile = null;
            if (authenticatedUserId.HasValue)
            {
                profile = await GetBookingFormProfileAsync(authenticatedUserId.Value, cancellationToken);
            }

            ServiceDto? selectedService = null;
            if (serviceId.HasValue && serviceId.Value > 0)
            {
                var service = await _unitOfWork.Repository<Service>().GetByIdAsync(serviceId.Value, cancellationToken);
                if (service != null && service.IsActive)
                {
                    var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
                    selectedService = new ServiceDto
                    {
                        Id = service.Id,
                        Name = service.Name,
                        Description = service.Description,
                        CategoryId = service.CategoryId,
                        CategoryName = category?.Name ?? string.Empty,
                        Price = service.Price,
                        FromLocation = service.FromLocation,
                        ToLocation = service.ToLocation,
                        ImageUrl = service.ImageUrl,
                        IsActive = service.IsActive,
                        AllowVehicleSelection = service.AllowVehicleSelection,
                        DisplayOrder = service.DisplayOrder,
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    };
                }
            }

            return new BookingFormInitDto
            {
                IsAuthenticated = authenticatedUserId.HasValue && profile != null,
                Profile = profile,
                SelectedService = selectedService
            };
        }

        public async Task<BookingRequestDto?> GetBookingByIdAsync(int id, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            // Authorization check: non-admin can only see their own booking
            if (!isAdmin)
            {
                if (!authenticatedUserId.HasValue || booking.Customer?.UserId != authenticatedUserId.Value)
                {
                    return null;
                }
            }

            return MapToDto(booking, isAdmin);
        }

        public async Task<BookingRequestDto?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                throw new InvalidOperationException("A valid booking reference is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByReferenceWithDetailsAsync(reference.Trim(), cancellationToken);
            return booking == null ? null : MapToDto(booking);
        }

        public async Task<IReadOnlyList<BookingRequestDto>> GetUserBookingsAsync(int authenticatedUserId, CancellationToken cancellationToken = default)
        {
            if (authenticatedUserId <= 0)
            {
                throw new InvalidOperationException("A valid user ID is required.");
            }

            var customer = await _unitOfWork.Repository<Customer>().FirstOrDefaultAsync(c => c.UserId == authenticatedUserId, cancellationToken);
            if (customer == null)
            {
                return new List<BookingRequestDto>();
            }

            var bookings = await _unitOfWork.BookingRequests.GetByCustomerIdWithDetailsAsync(customer.Id, cancellationToken);
            return bookings.Select(b => MapToDto(b, isAdmin: false)).ToList();
        }

        public async Task<IReadOnlyList<BookingRequestDto>> GetAllBookingsAsync(BookingStatus? status = null, CancellationToken cancellationToken = default)
        {
            var bookings = await _unitOfWork.BookingRequests.GetAllWithDetailsAsync(cancellationToken);
            if (status.HasValue)
            {
                bookings = bookings.Where(b => b.Status == status.Value).ToList();
            }
            return bookings.Select(b => MapToDto(b, isAdmin: true)).ToList();
        }

        public async Task<PagedResult<BookingRequestDto>> SearchBookingsAsync(BookingSearchFilterDto filter, CancellationToken cancellationToken = default)
        {
            if (filter == null)
            {
                filter = new BookingSearchFilterDto();
            }

            var coreFilter = new BookingSearchFilter
            {
                Search = filter.Search,
                CustomerName = filter.CustomerName,
                CustomerEmail = filter.CustomerEmail,
                CustomerPhone = filter.CustomerPhone,
                BookingId = filter.BookingId,
                BookingReference = filter.BookingReference,
                Status = filter.Status,
                ServiceId = filter.ServiceId,
                ServiceName = filter.ServiceName,
                VehicleId = filter.VehicleId,
                VehicleName = filter.VehicleName,
                Date = filter.Date,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            var (items, totalCount) = await _unitOfWork.BookingRequests.SearchBookingsAsync(coreFilter, cancellationToken);
            var dtos = items.Select(b => MapToDto(b, isAdmin: true)).ToList();

            return new PagedResult<BookingRequestDto>(dtos, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<BookingRequestDto?> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto dto, string changedBy, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            var oldStatus = booking.Status;
            if (oldStatus == dto.Status)
            {
                return MapToDto(booking);
            }

            // Validate status lifecycle transition
            if (!IsValidStatusTransition(oldStatus, dto.Status))
            {
                var allowed = GetAllowedTransitionsDescription(oldStatus);
                throw new InvalidOperationException($"Invalid booking status transition from '{oldStatus}' to '{dto.Status}'. Allowed transitions from '{oldStatus}' are: {allowed}.");
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

        public async Task<BookingRequestDto?> AcceptBookingAsync(int id, string changedBy, AcceptBookingDto? dto = null, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            // 1. Verify that current status allows acceptance (must be Pending)
            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Accepted)
            {
                throw new InvalidOperationException($"Only Pending bookings can be accepted. Current booking status is '{booking.Status}'.");
            }

            if (booking.Status == BookingStatus.Accepted)
            {
                return MapToDto(booking);
            }

            // 2. Validate required business rules
            // Verify selected service exists and is active
            var service = booking.Service ?? await _unitOfWork.Repository<Service>().GetByIdAsync(booking.ServiceId, cancellationToken);
            if (service == null || !service.IsActive)
            {
                throw new InvalidOperationException($"Service '{service?.Name ?? booking.ServiceNameSnapshot}' is inactive or no longer available.");
            }

            var category = service.Category ?? await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            if (category == null || !category.IsActive)
            {
                throw new InvalidOperationException($"The category for service '{service.Name}' is inactive.");
            }

            // Verify selected vehicle exists and is active
            var vehicle = booking.Vehicle ?? await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(booking.VehicleId, cancellationToken);
            if (vehicle == null || !vehicle.IsActive)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle?.Name ?? booking.VehicleNameSnapshot}' is inactive or no longer available.");
            }

            // Validate capacities
            if (booking.NumberOfPassengers > vehicle.PassengerCapacity)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' cannot accommodate {booking.NumberOfPassengers} passengers (maximum capacity: {vehicle.PassengerCapacity}).");
            }

            if (vehicle.LuggageCapacity > 0 && booking.NumberOfLuggage > vehicle.LuggageCapacity)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' cannot accommodate {booking.NumberOfLuggage} pieces of luggage (maximum capacity: {vehicle.LuggageCapacity}).");
            }

            // 3. Check vehicle availability according to configured availability rules
            var isAvailable = await _vehicleService.IsVehicleAvailableAsync(
                vehicle.Id, 
                booking.PickupDateTime, 
                booking.ReturnDateTime, 
                excludeBookingId: booking.Id, 
                cancellationToken: cancellationToken);

            if (!isAvailable)
            {
                throw new InvalidOperationException($"Vehicle '{vehicle.Name}' is not available for the requested time slot due to a scheduling conflict or required turnaround window.");
            }

            // 4. Booking status becomes Accepted
            var oldStatus = booking.Status;
            booking.Status = BookingStatus.Accepted;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BookingRequests.Update(booking);

            // 5. Status change recorded in booking history
            var notes = !string.IsNullOrWhiteSpace(dto?.Notes) ? dto.Notes.Trim() : "Booking request accepted by administrator.";
            var history = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = oldStatus,
                NewStatus = BookingStatus.Accepted,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = notes
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(history, cancellationToken);

            // 6. Trigger customer notification
            if (booking.Customer?.UserId != null)
            {
                var notification = new Notification
                {
                    UserId = booking.Customer.UserId,
                    Title = "Booking Accepted",
                    Message = $"Your booking {booking.BookingReference} for {booking.ServiceNameSnapshot} has been accepted.",
                    Type = "Booking",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var updatedBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            return MapToDto(updatedBooking ?? booking);
        }

        public async Task<BookingRequestDto?> RejectBookingAsync(int id, RejectBookingDto dto, string changedBy, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            // Only Pending bookings can be rejected (Admin refuses a request before it is accepted)
            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException($"Only Pending bookings can be rejected. Current status is '{booking.Status}'. Accepted bookings must be cancelled instead.");
            }

            var oldStatus = booking.Status;
            var reason = !string.IsNullOrWhiteSpace(dto.RejectionReason) 
                ? dto.RejectionReason.Trim() 
                : "Booking request rejected by administrator.";

            booking.Status = BookingStatus.Rejected;
            booking.RejectionReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BookingRequests.Update(booking);

            var history = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = oldStatus,
                NewStatus = BookingStatus.Rejected,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = reason
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(history, cancellationToken);

            if (booking.Customer?.UserId != null)
            {
                var notification = new Notification
                {
                    UserId = booking.Customer.UserId,
                    Title = "Booking Rejected",
                    Message = $"Your booking request {booking.BookingReference} was rejected. Reason: {reason}",
                    Type = "Booking",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var updatedBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            return MapToDto(updatedBooking ?? booking);
        }

        public async Task<BookingRequestDto?> CancelBookingAsync(int id, CancelBookingDto dto, string changedBy, int? authenticatedUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            // Authorization check
            if (!isAdmin)
            {
                if (!authenticatedUserId.HasValue || booking.Customer?.UserId != authenticatedUserId.Value)
                {
                    throw new UnauthorizedAccessException("You are not authorized to cancel this booking.");
                }
            }

            // Cancellation allowed for Pending, Accepted, Confirmed, or InProgress bookings
            if (booking.Status != BookingStatus.Pending && 
                booking.Status != BookingStatus.Accepted && 
                booking.Status != BookingStatus.Confirmed &&
                booking.Status != BookingStatus.InProgress)
            {
                throw new InvalidOperationException($"Booking with status '{booking.Status}' cannot be cancelled. Only Pending, Accepted, or In-Progress bookings can be cancelled.");
            }

            var oldStatus = booking.Status;
            var reason = !string.IsNullOrWhiteSpace(dto.CancellationReason) 
                ? dto.CancellationReason.Trim() 
                : "Booking cancelled.";

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BookingRequests.Update(booking);

            var history = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = oldStatus,
                NewStatus = BookingStatus.Cancelled,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = reason
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(history, cancellationToken);

            if (booking.Customer?.UserId != null)
            {
                var notification = new Notification
                {
                    UserId = booking.Customer.UserId,
                    Title = "Booking Cancelled",
                    Message = $"Booking {booking.BookingReference} has been cancelled. Reason: {reason}",
                    Type = "Booking",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var updatedBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            return MapToDto(updatedBooking ?? booking, isAdmin);
        }

        public async Task<BookingRequestDto?> CompleteBookingAsync(int id, string changedBy, CompleteBookingDto? dto = null, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new InvalidOperationException("A valid booking ID is required.");
            }

            var booking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            if (booking == null) return null;

            if (booking.Status == BookingStatus.Completed)
            {
                return MapToDto(booking, isAdmin: true);
            }

            // Only Accepted, Confirmed, or InProgress bookings can be marked as Completed
            if (booking.Status != BookingStatus.Accepted && 
                booking.Status != BookingStatus.Confirmed && 
                booking.Status != BookingStatus.InProgress)
            {
                throw new InvalidOperationException($"Only Accepted or In-Progress bookings can be marked as Completed. Current status is '{booking.Status}'.");
            }

            var oldStatus = booking.Status;
            var notes = !string.IsNullOrWhiteSpace(dto?.Notes) ? dto.Notes.Trim() : "Booking marked as completed by administrator.";

            booking.Status = BookingStatus.Completed;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BookingRequests.Update(booking);

            var history = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                PreviousStatus = oldStatus,
                NewStatus = BookingStatus.Completed,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Notes = notes
            };

            await _unitOfWork.Repository<BookingStatusHistory>().AddAsync(history, cancellationToken);

            if (booking.Customer?.UserId != null)
            {
                var notification = new Notification
                {
                    UserId = booking.Customer.UserId,
                    Title = "Trip Completed",
                    Message = $"Your booking {booking.BookingReference} for {booking.ServiceNameSnapshot} has been completed. Thank you for choosing our service!",
                    Type = "Booking",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var updatedBooking = await _unitOfWork.BookingRequests.GetByIdWithDetailsAsync(id, cancellationToken);
            return MapToDto(updatedBooking ?? booking, isAdmin: true);
        }

        private static BookingRequestDto MapToDto(BookingRequest booking, bool isAdmin = false)
        {
            var customerDto = booking.CustomerId.HasValue
                ? new CustomerDto
                {
                    Id = booking.CustomerId.Value,
                    FirstName = GetFirstName(booking.CustomerFullName, booking.Customer?.FirstName),
                    LastName = GetLastName(booking.CustomerFullName, booking.Customer?.LastName),
                    FullName = !string.IsNullOrWhiteSpace(booking.CustomerFullName) ? booking.CustomerFullName : (booking.Customer?.FullName ?? string.Empty),
                    Email = !string.IsNullOrWhiteSpace(booking.CustomerEmail) ? booking.CustomerEmail : (booking.Customer?.Email ?? string.Empty),
                    PhoneNumber = !string.IsNullOrWhiteSpace(booking.CustomerPhoneNumber) ? booking.CustomerPhoneNumber : (booking.Customer?.PhoneNumber ?? string.Empty),
                    Address = booking.Customer?.Address
                }
                : (!string.IsNullOrWhiteSpace(booking.CustomerFullName) || !string.IsNullOrWhiteSpace(booking.CustomerEmail) || !string.IsNullOrWhiteSpace(booking.CustomerPhoneNumber))
                    ? new CustomerDto
                    {
                        Id = 0,
                        FirstName = GetFirstName(booking.CustomerFullName, null),
                        LastName = GetLastName(booking.CustomerFullName, null),
                        FullName = booking.CustomerFullName ?? string.Empty,
                        Email = booking.CustomerEmail ?? string.Empty,
                        PhoneNumber = booking.CustomerPhoneNumber ?? string.Empty,
                        Address = null
                    }
                    : null;

            return new BookingRequestDto
            {
                Id = booking.Id,
                BookingReference = booking.BookingReference,
                CustomerId = booking.CustomerId,
                Customer = customerDto,
                CustomerName = booking.CustomerFullName ?? string.Empty,
                CustomerEmail = booking.CustomerEmail ?? string.Empty,
                CustomerPhone = booking.CustomerPhoneNumber ?? string.Empty,
                ServiceId = booking.ServiceId,
                ServiceName = !string.IsNullOrWhiteSpace(booking.ServiceNameSnapshot) ? booking.ServiceNameSnapshot : (booking.Service?.Name ?? string.Empty),
                ServiceNameSnapshot = !string.IsNullOrWhiteSpace(booking.ServiceNameSnapshot) ? booking.ServiceNameSnapshot : (booking.Service?.Name ?? string.Empty),
                CategoryName = booking.Service?.Category?.Name ?? string.Empty,
                VehicleId = booking.VehicleId,
                VehicleName = !string.IsNullOrWhiteSpace(booking.VehicleNameSnapshot) ? booking.VehicleNameSnapshot : (booking.Vehicle != null ? (!string.IsNullOrWhiteSpace(booking.Vehicle.Name) ? booking.Vehicle.Name : $"{booking.Vehicle.Make} {booking.Vehicle.Model}".Trim()) : string.Empty),
                VehicleNameSnapshot = !string.IsNullOrWhiteSpace(booking.VehicleNameSnapshot) ? booking.VehicleNameSnapshot : (booking.Vehicle != null ? (!string.IsNullOrWhiteSpace(booking.Vehicle.Name) ? booking.Vehicle.Name : $"{booking.Vehicle.Make} {booking.Vehicle.Model}".Trim()) : string.Empty),
                VehicleLicensePlate = booking.Vehicle?.LicensePlate ?? string.Empty,
                VehicleImageUrl = booking.Vehicle?.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl ?? booking.Vehicle?.Images?.FirstOrDefault()?.ImageUrl,
                PickupLocation = booking.PickupLocation,
                DropoffLocation = booking.DropoffLocation,
                PickupDateTime = booking.PickupDateTime,
                PickupDate = booking.PickupDateTime.ToString("yyyy-MM-dd"),
                PickupTime = booking.PickupDateTime.ToString("HH:mm"),
                ReturnDateTime = booking.ReturnDateTime,
                FlightNumber = booking.FlightNumber,
                NumberOfPassengers = booking.NumberOfPassengers,
                NumberOfLuggage = booking.NumberOfLuggage,
                SpecialNotes = booking.SpecialNotes,
                CustomerNotes = booking.SpecialNotes,
                EstimatedPrice = booking.EstimatedPrice,
                Status = booking.Status,
                RejectionReason = booking.RejectionReason,
                CancellationReason = booking.CancellationReason,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                StatusHistories = booking.StatusHistories?.Select(h => new BookingStatusHistoryDto
                {
                    Id = h.Id,
                    BookingId = h.BookingRequestId,
                    BookingRequestId = h.BookingRequestId,
                    OldStatus = h.PreviousStatus,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    CreatedAt = h.ChangedAt,
                    ChangedAt = h.ChangedAt,
                    ChangedBy = isAdmin ? h.ChangedBy : (h.ChangedBy != null && h.ChangedBy.Contains("@") ? "Support Team" : h.ChangedBy),
                    Reason = h.Notes,
                    Notes = isAdmin ? h.Notes : (h.NewStatus == BookingStatus.Rejected || h.NewStatus == BookingStatus.Cancelled ? h.Notes : null)
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
