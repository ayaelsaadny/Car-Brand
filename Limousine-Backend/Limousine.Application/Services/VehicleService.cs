using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Enums;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public VehicleService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var vehicles = await _unitOfWork.Vehicles.GetAllWithImagesAsync(activeOnly ?? false, cancellationToken);
            return vehicles.Select(MapToDto).ToList();
        }

        public async Task<VehicleDto?> GetByIdAsync(int id, bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(id, cancellationToken);
            if (vehicle == null) return null;

            if (activeOnly.HasValue && activeOnly.Value && !vehicle.IsActive)
            {
                return null;
            }

            return MapToDto(vehicle);
        }

        public async Task<VehicleDto?> GetDefaultVehicleAsync(CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetDefaultVehicleAsync(cancellationToken);
            return vehicle == null ? null : MapToDto(vehicle);
        }

        public async Task<DefaultVehicleDto?> GetDefaultVehicleWithAvailabilityAsync(DateTime? pickupDateTime = null, DateTime? returnDateTime = null, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetDefaultVehicleAsync(cancellationToken);
            if (vehicle == null) return null;

            bool? isAvailable = null;
            string? unavailabilityReason = null;

            if (pickupDateTime.HasValue)
            {
                var available = await IsVehicleAvailableAsync(vehicle.Id, pickupDateTime.Value, returnDateTime, cancellationToken: cancellationToken);
                isAvailable = available;
                if (!available)
                {
                    unavailabilityReason = "Vehicle is reserved during the requested time window. Default status does not guarantee availability.";
                }
            }

            return MapToDefaultDto(vehicle, isAvailable, unavailabilityReason);
        }

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.IsDefault && !dto.IsActive)
            {
                throw new InvalidOperationException("Cannot set an inactive vehicle as the default vehicle.");
            }

            var existingWithPlate = await _unitOfWork.Vehicles
                .FirstOrDefaultAsync(v => v.LicensePlate.ToLower() == dto.LicensePlate.Trim().ToLower(), cancellationToken);

            if (existingWithPlate != null)
            {
                throw new InvalidOperationException($"A vehicle with license plate '{dto.LicensePlate}' already exists.");
            }

            var totalVehicles = await _unitOfWork.Vehicles.CountAsync(v => true, cancellationToken);
            var shouldBeDefault = dto.IsDefault || totalVehicles == 0;

            var vehicle = new Vehicle
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Make = dto.Make.Trim(),
                Model = dto.Model.Trim(),
                Year = dto.Year,
                LicensePlate = dto.LicensePlate.Trim().ToUpperInvariant(),
                Color = dto.Color?.Trim(),
                PassengerCapacity = dto.PassengerCapacity,
                LuggageCapacity = dto.LuggageCapacity,
                PricePerHour = dto.PricePerHour,
                PricePerKm = dto.PricePerKm,
                Features = dto.Features?.Trim(),
                IsActive = dto.IsActive,
                IsDefault = shouldBeDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            if (dto.Images != null && dto.Images.Count > 0)
            {
                var hasPrimary = dto.Images.Any(i => i.IsPrimary);
                var primaryAssigned = false;

                for (var i = 0; i < dto.Images.Count; i++)
                {
                    var imgDto = dto.Images[i];
                    var isPrimary = false;

                    if (hasPrimary)
                    {
                        if (imgDto.IsPrimary && !primaryAssigned)
                        {
                            isPrimary = true;
                            primaryAssigned = true;
                        }
                    }
                    else if (i == 0)
                    {
                        isPrimary = true;
                        primaryAssigned = true;
                    }

                    vehicle.Images.Add(new VehicleImage
                    {
                        ImageUrl = imgDto.ImageUrl.Trim(),
                        IsPrimary = isPrimary,
                        DisplayOrder = imgDto.DisplayOrder != 0 ? imgDto.DisplayOrder : i + 1,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.Vehicles.AddAsync(vehicle, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            if (shouldBeDefault)
            {
                await _unitOfWork.Vehicles.SetDefaultVehicleAsync(vehicle.Id, cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);
            }

            var created = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicle.Id, cancellationToken);
            return MapToDto(created ?? vehicle);
        }

        public async Task<VehicleDto?> UpdateAsync(int id, UpdateVehicleDto dto, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(id, cancellationToken);
            if (vehicle == null) return null;

            var duplicatePlate = await _unitOfWork.Vehicles
                .FirstOrDefaultAsync(v => v.Id != id && v.LicensePlate.ToLower() == dto.LicensePlate.Trim().ToLower(), cancellationToken);

            if (duplicatePlate != null)
            {
                throw new InvalidOperationException($"Another vehicle with license plate '{dto.LicensePlate}' already exists.");
            }

            if (dto.IsDefault && !dto.IsActive)
            {
                throw new InvalidOperationException("Cannot set an inactive vehicle as the default vehicle.");
            }

            // If deactivating the default vehicle, fallback default to another active vehicle
            if (!dto.IsActive && vehicle.IsDefault)
            {
                vehicle.IsDefault = false;
                var nextActive = await _unitOfWork.Vehicles.FirstOrDefaultAsync(v => v.Id != id && v.IsActive, cancellationToken);
                if (nextActive != null)
                {
                    nextActive.IsDefault = true;
                    _unitOfWork.Vehicles.Update(nextActive);
                }
            }

            vehicle.Name = dto.Name.Trim();
            vehicle.Description = dto.Description?.Trim();
            vehicle.Make = dto.Make.Trim();
            vehicle.Model = dto.Model.Trim();
            vehicle.Year = dto.Year;
            vehicle.LicensePlate = dto.LicensePlate.Trim().ToUpperInvariant();
            vehicle.Color = dto.Color?.Trim();
            vehicle.PassengerCapacity = dto.PassengerCapacity;
            vehicle.LuggageCapacity = dto.LuggageCapacity;
            vehicle.PricePerHour = dto.PricePerHour;
            vehicle.PricePerKm = dto.PricePerKm;
            vehicle.Features = dto.Features?.Trim();
            vehicle.IsActive = dto.IsActive;
            vehicle.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);

            if (dto.IsDefault && dto.IsActive)
            {
                await _unitOfWork.Vehicles.SetDefaultVehicleAsync(vehicle.Id, cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);
            }

            var updated = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicle.Id, cancellationToken);
            return MapToDto(updated ?? vehicle);
        }

        public async Task<VehicleDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(id, cancellationToken);
            if (vehicle == null) return null;

            vehicle.IsActive = isActive;
            vehicle.UpdatedAt = DateTime.UtcNow;

            // If deactivating the default vehicle, fallback default to another active vehicle
            if (!isActive && vehicle.IsDefault)
            {
                vehicle.IsDefault = false;
                var nextActive = await _unitOfWork.Vehicles.FirstOrDefaultAsync(v => v.Id != id && v.IsActive, cancellationToken);
                if (nextActive != null)
                {
                    nextActive.IsDefault = true;
                    _unitOfWork.Vehicles.Update(nextActive);
                }
            }

            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return MapToDto(vehicle);
        }

        public async Task<bool> SetDefaultAsync(int id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id, cancellationToken);
            if (vehicle == null) return false;

            if (!vehicle.IsActive)
            {
                throw new InvalidOperationException("Cannot set an inactive vehicle as the default vehicle.");
            }

            await _unitOfWork.Vehicles.SetDefaultVehicleAsync(id, cancellationToken);
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<VehicleImageDto>> GetVehicleImagesAsync(int vehicleId, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null)
            {
                return new List<VehicleImageDto>();
            }

            return vehicle.Images
                .OrderBy(img => img.DisplayOrder)
                .ThenBy(img => img.Id)
                .Select(img => new VehicleImageDto
                {
                    Id = img.Id,
                    VehicleId = img.VehicleId,
                    ImageUrl = img.ImageUrl,
                    IsPrimary = img.IsPrimary,
                    DisplayOrder = img.DisplayOrder,
                    CreatedAt = img.CreatedAt
                }).ToList();
        }

        public async Task<VehicleImageDto?> GetPrimaryImageAsync(int vehicleId, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null || !vehicle.Images.Any())
            {
                return null;
            }

            var primary = vehicle.Images.FirstOrDefault(i => i.IsPrimary)
                          ?? vehicle.Images.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id).FirstOrDefault();

            if (primary == null) return null;

            return new VehicleImageDto
            {
                Id = primary.Id,
                VehicleId = primary.VehicleId,
                ImageUrl = primary.ImageUrl,
                IsPrimary = primary.IsPrimary,
                DisplayOrder = primary.DisplayOrder,
                CreatedAt = primary.CreatedAt
            };
        }

        public async Task<IReadOnlyList<VehicleImageDto>> UploadImagesAsync(int vehicleId, IEnumerable<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null)
            {
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} does not exist.");
            }

            var maxDisplayOrder = vehicle.Images.Any() ? vehicle.Images.Max(i => i.DisplayOrder) : 0;
            var hasPrimaryImage = vehicle.Images.Any(i => i.IsPrimary);

            var addedImages = new List<VehicleImage>();
            var index = 0;

            foreach (var (stream, fileName) in files)
            {
                var uploadedUrl = await _fileService.SaveFileAsync(stream, fileName, "vehicles", cancellationToken);
                var isPrimary = !hasPrimaryImage && index == 0;

                var image = new VehicleImage
                {
                    VehicleId = vehicleId,
                    ImageUrl = uploadedUrl,
                    IsPrimary = isPrimary,
                    DisplayOrder = maxDisplayOrder + index + 1,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<VehicleImage>().AddAsync(image, cancellationToken);
                vehicle.Images.Add(image);
                addedImages.Add(image);
                index++;
            }

            vehicle.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return addedImages.Select(img => new VehicleImageDto
            {
                Id = img.Id,
                VehicleId = img.VehicleId,
                ImageUrl = img.ImageUrl,
                IsPrimary = img.IsPrimary,
                DisplayOrder = img.DisplayOrder,
                CreatedAt = img.CreatedAt
            }).ToList();
        }

        public async Task<VehicleImageDto> AddImageAsync(int vehicleId, CreateVehicleImageDto dto, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null)
            {
                throw new InvalidOperationException($"Vehicle with ID {vehicleId} does not exist.");
            }

            var shouldBePrimary = dto.IsPrimary || !vehicle.Images.Any();

            if (shouldBePrimary)
            {
                foreach (var existing in vehicle.Images)
                {
                    if (existing.IsPrimary)
                    {
                        existing.IsPrimary = false;
                        _unitOfWork.Repository<VehicleImage>().Update(existing);
                    }
                }
            }

            var maxDisplayOrder = vehicle.Images.Any() ? vehicle.Images.Max(i => i.DisplayOrder) : 0;
            var displayOrder = dto.DisplayOrder != 0 ? dto.DisplayOrder : maxDisplayOrder + 1;

            var image = new VehicleImage
            {
                VehicleId = vehicleId,
                ImageUrl = dto.ImageUrl.Trim(),
                IsPrimary = shouldBePrimary,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<VehicleImage>().AddAsync(image, cancellationToken);
            vehicle.Images.Add(image);
            vehicle.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new VehicleImageDto
            {
                Id = image.Id,
                VehicleId = image.VehicleId,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder,
                CreatedAt = image.CreatedAt
            };
        }

        public async Task<bool> SetPrimaryImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null) return false;

            var targetImage = vehicle.Images.FirstOrDefault(i => i.Id == imageId);
            if (targetImage == null) return false;

            foreach (var img in vehicle.Images)
            {
                var shouldBePrimary = (img.Id == imageId);
                if (img.IsPrimary != shouldBePrimary)
                {
                    img.IsPrimary = shouldBePrimary;
                    _unitOfWork.Repository<VehicleImage>().Update(img);
                }
            }

            vehicle.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }

        public Task<bool> SetMainImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default)
        {
            return SetPrimaryImageAsync(vehicleId, imageId, cancellationToken);
        }

        public async Task<bool> ReorderImagesAsync(int vehicleId, List<VehicleImageOrderDto> orders, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null) return false;

            var orderMap = orders.ToDictionary(o => o.ImageId, o => o.DisplayOrder);
            var modified = false;

            foreach (var img in vehicle.Images)
            {
                if (orderMap.TryGetValue(img.Id, out var newOrder) && img.DisplayOrder != newOrder)
                {
                    img.DisplayOrder = newOrder;
                    _unitOfWork.Repository<VehicleImage>().Update(img);
                    modified = true;
                }
            }

            if (modified)
            {
                vehicle.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Vehicles.Update(vehicle);
                await _unitOfWork.CompleteAsync(cancellationToken);
            }

            return true;
        }

        public async Task<bool> DeleteImageAsync(int vehicleId, int imageId, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(vehicleId, cancellationToken);
            if (vehicle == null) return false;

            var image = vehicle.Images.FirstOrDefault(i => i.Id == imageId);
            if (image == null) return false;

            var wasPrimary = image.IsPrimary;

            // Delete local file if present
            _fileService.DeleteFile(image.ImageUrl);

            _unitOfWork.Repository<VehicleImage>().Delete(image);
            vehicle.Images.Remove(image);

            // If primary was deleted, auto-promote next available image
            if (wasPrimary && vehicle.Images.Any())
            {
                var nextPrimary = vehicle.Images.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id).First();
                nextPrimary.IsPrimary = true;
                _unitOfWork.Repository<VehicleImage>().Update(nextPrimary);
            }

            vehicle.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Vehicles.Update(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteImageAsync(int imageId, CancellationToken cancellationToken = default)
        {
            var image = await _unitOfWork.Repository<VehicleImage>().GetByIdAsync(imageId, cancellationToken);
            if (image == null) return false;

            return await DeleteImageAsync(image.VehicleId, imageId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithImagesAsync(id, cancellationToken);
            if (vehicle == null) return false;

            if (archiveOnly)
            {
                // Soft delete / archive
                vehicle.IsActive = false;
                vehicle.UpdatedAt = DateTime.UtcNow;

                if (vehicle.IsDefault)
                {
                    vehicle.IsDefault = false;
                    var nextActive = await _unitOfWork.Vehicles.FirstOrDefaultAsync(v => v.Id != id && v.IsActive, cancellationToken);
                    if (nextActive != null)
                    {
                        nextActive.IsDefault = true;
                        _unitOfWork.Vehicles.Update(nextActive);
                    }
                }

                _unitOfWork.Vehicles.Update(vehicle);
                await _unitOfWork.CompleteAsync(cancellationToken);
                return true;
            }

            // Hard delete request: Check for associated bookings
            var hasBookings = await _unitOfWork.BookingRequests.AnyAsync(b => b.VehicleId == id, cancellationToken);
            if (hasBookings)
            {
                throw new InvalidOperationException("Cannot permanently delete vehicle with associated bookings. Deactivate/archive the vehicle instead.");
            }

            // Delete associated image files
            if (vehicle.Images != null)
            {
                foreach (var img in vehicle.Images)
                {
                    _fileService.DeleteFile(img.ImageUrl);
                }
            }

            _unitOfWork.Vehicles.Delete(vehicle);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }

        public async Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime? returnDateTime, int? excludeBookingId = null, CancellationToken cancellationToken = default)
        {
            var reqStart = pickupDateTime;
            var reqEnd = returnDateTime ?? pickupDateTime.AddHours(2);

            // Buffer time: 30 minutes before and after for turnaround/cleaning/positioning
            var bufferStart = reqStart.AddMinutes(-30);
            var bufferEnd = reqEnd.AddMinutes(30);

            var bookings = await _unitOfWork.BookingRequests.GetAllAsync(cancellationToken);
            var conflicting = bookings.Any(b =>
                b.VehicleId == vehicleId &&
                (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value) &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected &&
                b.PickupDateTime < bufferEnd &&
                (b.ReturnDateTime ?? b.PickupDateTime.AddHours(2)) > bufferStart);

            return !conflicting;
        }

        public async Task<VehicleAvailabilityResponseDto> GetAvailableVehiclesAsync(AvailableVehiclesQueryDto query, CancellationToken cancellationToken = default)
        {
            var activeVehicles = await _unitOfWork.Vehicles.GetAllWithImagesAsync(activeOnly: true, cancellationToken);

            bool vehicleSelectionEnabled = true;
            if (query.ServiceId.HasValue && query.ServiceId.Value > 0)
            {
                var service = await _unitOfWork.Repository<Service>().GetByIdAsync(query.ServiceId.Value, cancellationToken);
                if (service != null)
                {
                    vehicleSelectionEnabled = service.AllowVehicleSelection;
                }
            }

            var isSingleActiveVehicle = activeVehicles.Count == 1;

            var availableList = new List<AvailableVehicleDto>();

            foreach (var vehicle in activeVehicles)
            {
                var vehicleDto = MapToDto(vehicle);

                // 1. Eligibility Check
                var isEligible = true;
                string? ineligibilityReason = null;

                if (query.NumberOfPassengers > vehicle.PassengerCapacity)
                {
                    isEligible = false;
                    ineligibilityReason = $"Exceeds passenger capacity (Requested: {query.NumberOfPassengers}, Max: {vehicle.PassengerCapacity})";
                }
                else if (vehicle.LuggageCapacity > 0 && query.NumberOfLuggage > vehicle.LuggageCapacity)
                {
                    isEligible = false;
                    ineligibilityReason = $"Exceeds luggage capacity (Requested: {query.NumberOfLuggage}, Max: {vehicle.LuggageCapacity})";
                }

                // 2. Availability Check
                var isAvailable = await IsVehicleAvailableAsync(vehicle.Id, query.PickupDateTime, query.ReturnDateTime, cancellationToken: cancellationToken);
                string? unavailabilityReason = isAvailable ? null : "Vehicle is reserved during the requested time window.";

                var canBeSelected = isEligible && isAvailable && vehicleSelectionEnabled;

                availableList.Add(new AvailableVehicleDto
                {
                    Vehicle = vehicleDto,
                    IsEligible = isEligible,
                    IsAvailable = isAvailable,
                    CanBeSelected = canBeSelected,
                    IneligibilityReason = ineligibilityReason,
                    UnavailabilityReason = unavailabilityReason
                });

            }

            // The default vehicle is the vehicle initially selected/displayed when a vehicle choice is required.
            // Order availableList with the default vehicle displayed first:
            var sortedVehicles = availableList
                .OrderByDescending(v => v.Vehicle.IsDefault)
                .ThenBy(v => v.Vehicle.Name)
                .ToList();

            var defaultActiveVehicle = activeVehicles.FirstOrDefault(v => v.IsDefault) ?? activeVehicles.FirstOrDefault();
            VehicleDto? defaultVehicleDto = defaultActiveVehicle != null ? MapToDto(defaultActiveVehicle) : null;

            var defaultItem = defaultActiveVehicle != null 
                ? sortedVehicles.FirstOrDefault(v => v.Vehicle.Id == defaultActiveVehicle.Id)
                : null;

            var isDefaultAvailable = defaultItem?.IsAvailable ?? false;
            var isDefaultEligible = defaultItem?.IsEligible ?? false;

            VehicleDto? initiallySelectedVehicle = null;
            VehicleDto? selectedVehicle = null;

            if (isSingleActiveVehicle)
            {
                // Single vehicle: automatically selected
                selectedVehicle = defaultVehicleDto;
                initiallySelectedVehicle = (isDefaultAvailable && isDefaultEligible) ? defaultVehicleDto : null;
            }
            else
            {
                // Multiple vehicles:
                // Default status does not automatically mean that the vehicle is available at every date/time.
                // The system must not assume permanent availability based only on IsDefault.
                if (isDefaultAvailable && isDefaultEligible)
                {
                    initiallySelectedVehicle = defaultVehicleDto;
                    if (!vehicleSelectionEnabled)
                    {
                        selectedVehicle = defaultVehicleDto;
                    }
                }
                else if (!vehicleSelectionEnabled)
                {
                    // Selection disabled: fallback auto-assign to first eligible & available vehicle
                    var fallback = sortedVehicles.FirstOrDefault(v => v.IsEligible && v.IsAvailable);
                    selectedVehicle = fallback?.Vehicle;
                }
            }

            return new VehicleAvailabilityResponseDto
            {
                AutoSelected = isSingleActiveVehicle,
                VehicleSelectionEnabled = vehicleSelectionEnabled,
                DefaultVehicle = defaultVehicleDto,
                IsDefaultVehicleAvailable = isDefaultAvailable,
                InitiallySelectedVehicle = initiallySelectedVehicle,
                SelectedVehicle = selectedVehicle,
                AvailableVehicles = sortedVehicles
            };
        }

        private static DefaultVehicleDto MapToDefaultDto(Vehicle vehicle, bool? isAvailable = null, string? unavailabilityReason = null)
        {
            var baseDto = MapToDto(vehicle);
            return new DefaultVehicleDto
            {
                Id = baseDto.Id,
                Name = baseDto.Name,
                Description = baseDto.Description,
                Make = baseDto.Make,
                Model = baseDto.Model,
                Year = baseDto.Year,
                LicensePlate = baseDto.LicensePlate,
                Color = baseDto.Color,
                PassengerCapacity = baseDto.PassengerCapacity,
                LuggageCapacity = baseDto.LuggageCapacity,
                PricePerHour = baseDto.PricePerHour,
                PricePerKm = baseDto.PricePerKm,
                Features = baseDto.Features,
                IsActive = baseDto.IsActive,
                IsDefault = baseDto.IsDefault,
                CreatedAt = baseDto.CreatedAt,
                UpdatedAt = baseDto.UpdatedAt,
                Images = baseDto.Images,
                IsAvailable = isAvailable,
                UnavailabilityReason = unavailabilityReason
            };
        }

        private static VehicleDto MapToDto(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                Name = vehicle.Name,
                Description = vehicle.Description,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                PassengerCapacity = vehicle.PassengerCapacity,
                LuggageCapacity = vehicle.LuggageCapacity,
                PricePerHour = vehicle.PricePerHour,
                PricePerKm = vehicle.PricePerKm,
                Features = vehicle.Features,
                IsActive = vehicle.IsActive,
                IsDefault = vehicle.IsDefault,
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt,
                Images = vehicle.Images?
                    .OrderBy(img => img.DisplayOrder)
                    .ThenBy(img => img.Id)
                    .Select(img => new VehicleImageDto
                    {
                        Id = img.Id,
                        VehicleId = img.VehicleId,
                        ImageUrl = img.ImageUrl,
                        IsPrimary = img.IsPrimary,
                        DisplayOrder = img.DisplayOrder,
                        CreatedAt = img.CreatedAt
                    }).ToList() ?? new List<VehicleImageDto>()
            };
        }
    }
}
