using Limousine.Application.DTOs;
using Limousine.Application.Interfaces;
using Limousine.Core.Entities;
using Limousine.Core.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.Application.Services
{
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public ServiceManagementService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IReadOnlyList<ServiceDto>> GetAllAsync(int? categoryId = null, string? searchTerm = null, bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(new ServiceFilterQueryDto
            {
                CategoryId = categoryId,
                Search = searchTerm,
                ActiveOnly = activeOnly
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<ServiceDto>> GetAllAsync(ServiceFilterQueryDto filter, CancellationToken cancellationToken = default)
        {
            var services = await _unitOfWork.Repository<Service>().GetAllAsync(cancellationToken);
            var categories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
            var activeCategoryIds = categories.Where(c => c.IsActive).Select(c => c.Id).ToHashSet();
            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

            var query = services.AsEnumerable();

            // Business rule: Only active services under active categories should be returned for public display
            var effectiveActiveOnly = filter.ActiveOnly ?? true;
            if (effectiveActiveOnly)
            {
                query = query.Where(s => s.IsActive && activeCategoryIds.Contains(s.CategoryId));
            }

            // 1. Filter by Category ID
            if (filter.CategoryId.HasValue)
            {
                if (effectiveActiveOnly && !activeCategoryIds.Contains(filter.CategoryId.Value))
                {
                    return Array.Empty<ServiceDto>();
                }
                query = query.Where(s => s.CategoryId == filter.CategoryId.Value);
            }

            // 2. Filter by Category Name (case-insensitive)
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var catNameFilter = filter.Category.Trim().ToLowerInvariant();
                var matchingCatIds = categories
                    .Where(c => (!effectiveActiveOnly || c.IsActive) && c.Name.ToLowerInvariant().Contains(catNameFilter))
                    .Select(c => c.Id)
                    .ToHashSet();

                if (!matchingCatIds.Any())
                {
                    return Array.Empty<ServiceDto>();
                }

                query = query.Where(s => matchingCatIds.Contains(s.CategoryId));
            }

            // 3. Search: Service Name, Service Description, Category Name (case-insensitive)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    (s.Name != null && s.Name.ToLowerInvariant().Contains(term)) ||
                    (s.Description != null && s.Description.ToLowerInvariant().Contains(term)) ||
                    (categoryDict.TryGetValue(s.CategoryId, out var catName) && catName.ToLowerInvariant().Contains(term)) ||
                    (s.FromLocation != null && s.FromLocation.ToLowerInvariant().Contains(term)) ||
                    (s.ToLocation != null && s.ToLocation.ToLowerInvariant().Contains(term)));
            }

            // 4. Optional Additional Filters
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(s => s.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(s => s.Price <= filter.MaxPrice.Value);
            }

            if (filter.AllowVehicleSelection.HasValue)
            {
                query = query.Where(s => s.AllowVehicleSelection == filter.AllowVehicleSelection.Value);
            }

            // 5. Sorting (default: DisplayOrder ASC, Name ASC)
            query = (filter.SortBy?.ToLowerInvariant()) switch
            {
                "priceasc" or "price_asc" or "price" => query.OrderBy(s => s.Price ?? decimal.MaxValue).ThenBy(s => s.DisplayOrder),
                "pricedesc" or "price_desc" => query.OrderByDescending(s => s.Price ?? 0).ThenBy(s => s.DisplayOrder),
                "name" or "nameasc" => query.OrderBy(s => s.Name),
                "namedesc" => query.OrderByDescending(s => s.Name),
                _ => query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            };

            var sorted = query.ToList();

            var bookings = await _unitOfWork.BookingRequests.GetAllAsync(cancellationToken);
            var bookingCounts = bookings
                .GroupBy(b => b.ServiceId)
                .ToDictionary(g => g.Key, g => g.Count());

            return sorted.Select(s => new ServiceDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                CategoryName = categoryDict.TryGetValue(s.CategoryId, out var name) ? name : string.Empty,
                Name = s.Name,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                Price = s.Price,
                FromLocation = s.FromLocation,
                ToLocation = s.ToLocation,
                IsActive = s.IsActive,
                AllowVehicleSelection = s.AllowVehicleSelection,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                TotalBookings = bookingCounts.TryGetValue(s.Id, out var count) ? count : 0
            }).ToList();
        }

        public async Task<ServiceDto?> GetByIdAsync(int id, bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            // Direct access rule: Inactive service is hidden from public
            if (activeOnly.HasValue && activeOnly.Value && !service.IsActive)
            {
                return null;
            }

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(dto.CategoryId, cancellationToken);
            if (category == null)
            {
                throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");
            }

            var existingService = await _unitOfWork.Repository<Service>()
                .FirstOrDefaultAsync(s => s.CategoryId == dto.CategoryId && s.Name.ToLower() == dto.Name.Trim().ToLower(), cancellationToken);

            if (existingService != null)
            {
                throw new InvalidOperationException($"A service with the name '{dto.Name}' already exists in this category.");
            }

            var service = new Service
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                ImageUrl = dto.ImageUrl?.Trim() ?? string.Empty,
                Price = dto.Price,
                FromLocation = dto.FromLocation?.Trim(),
                ToLocation = dto.ToLocation?.Trim(),
                IsActive = dto.IsActive,
                AllowVehicleSelection = dto.AllowVehicleSelection,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _unitOfWork.Repository<Service>().AddAsync(service, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category.Name,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                AllowVehicleSelection = service.AllowVehicleSelection,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = 0
            };
        }

        public async Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(dto.CategoryId, cancellationToken);
            if (category == null)
            {
                throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");
            }

            var duplicate = await _unitOfWork.Repository<Service>()
                .FirstOrDefaultAsync(s => s.Id != id && s.CategoryId == dto.CategoryId && s.Name.ToLower() == dto.Name.Trim().ToLower(), cancellationToken);

            if (duplicate != null)
            {
                throw new InvalidOperationException($"Another service with the name '{dto.Name}' already exists in this category.");
            }

            service.CategoryId = dto.CategoryId;
            service.Name = dto.Name.Trim();
            service.Description = dto.Description?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                service.ImageUrl = dto.ImageUrl.Trim();
            }
            service.Price = dto.Price;
            service.FromLocation = dto.FromLocation?.Trim();
            service.ToLocation = dto.ToLocation?.Trim();
            service.IsActive = dto.IsActive;
            service.AllowVehicleSelection = dto.AllowVehicleSelection;
            service.DisplayOrder = dto.DisplayOrder;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category.Name,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                AllowVehicleSelection = service.AllowVehicleSelection,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<ServiceDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            service.IsActive = isActive;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<ServiceDto?> UpdateDisplayOrderAsync(int id, int displayOrder, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            service.DisplayOrder = displayOrder;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<ServiceDto?> UploadImageAsync(int id, Stream imageStream, string fileName, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            var oldImageUrl = service.ImageUrl;

            var newImageUrl = await _fileService.SaveFileAsync(imageStream, fileName, "services", cancellationToken);
            service.ImageUrl = newImageUrl;
            service.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Service>().Update(service);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Clean up old uploaded image if it was stored in local uploads
            if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl.StartsWith("/uploads/services/"))
            {
                _fileService.DeleteFile(oldImageUrl);
            }

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);
            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = category?.Name ?? string.Empty,
                Name = service.Name,
                Description = service.Description,
                ImageUrl = service.ImageUrl,
                Price = service.Price,
                FromLocation = service.FromLocation,
                ToLocation = service.ToLocation,
                IsActive = service.IsActive,
                AllowVehicleSelection = service.AllowVehicleSelection,
                DisplayOrder = service.DisplayOrder,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt,
                TotalBookings = bookingCount
            };
        }

        public async Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return false;

            if (archiveOnly)
            {
                // Soft delete / archive: Preserves existing historical bookings automatically
                service.IsActive = false;
                service.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Service>().Update(service);
                await _unitOfWork.CompleteAsync(cancellationToken);
                return true;
            }

            // Hard delete request: Enforce business rule to protect historical bookings
            var hasBookings = await _unitOfWork.BookingRequests.AnyAsync(b => b.ServiceId == id, cancellationToken);
            if (hasBookings)
            {
                throw new InvalidOperationException("Cannot permanently delete service with associated historical bookings. Archive/deactivate the service instead.");
            }

            _unitOfWork.Repository<Service>().Delete(service);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }
    }
}
