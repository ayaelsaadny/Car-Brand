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
        private readonly ICurrentLanguageService _languageService;
        private readonly ILocalizationService _localizationService;

        public ServiceManagementService(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            ICurrentLanguageService languageService,
            ILocalizationService localizationService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _languageService = languageService;
            _localizationService = localizationService;
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
            var serviceTranslations = await _unitOfWork.Repository<ServiceTranslation>().GetAllAsync(cancellationToken);
            var categoryTranslations = await _unitOfWork.Repository<CategoryTranslation>().GetAllAsync(cancellationToken);

            var currentLang = _languageService.CurrentLanguage;

            var serviceTransLookup = serviceTranslations
                .GroupBy(t => t.ServiceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var catTransLookup = categoryTranslations
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            string ResolveCatName(Category c)
            {
                if (catTransLookup.TryGetValue(c.Id, out var transList))
                {
                    if (currentLang == "fr")
                    {
                        var fr = transList.FirstOrDefault(t => t.LanguageCode == "fr");
                        if (fr != null && !string.IsNullOrWhiteSpace(fr.Name)) return fr.Name;
                    }
                    var en = transList.FirstOrDefault(t => t.LanguageCode == "en");
                    if (en != null && !string.IsNullOrWhiteSpace(en.Name)) return en.Name;
                }
                return c.Name;
            }

            var activeCategoryIds = categories.Where(c => c.IsActive).Select(c => c.Id).ToHashSet();
            var categoryDict = categories.ToDictionary(c => c.Id, c => ResolveCatName(c));

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

            (string Name, string? Description) ResolveServiceText(Service s)
            {
                if (serviceTransLookup.TryGetValue(s.Id, out var transList))
                {
                    if (currentLang == "fr")
                    {
                        var fr = transList.FirstOrDefault(t => t.LanguageCode == "fr");
                        if (fr != null && !string.IsNullOrWhiteSpace(fr.Name))
                        {
                            return (fr.Name, fr.Description ?? s.Description);
                        }
                    }
                    var en = transList.FirstOrDefault(t => t.LanguageCode == "en");
                    if (en != null && !string.IsNullOrWhiteSpace(en.Name))
                    {
                        return (en.Name, en.Description ?? s.Description);
                    }
                }
                return (s.Name, s.Description);
            }

            return sorted.Select(s =>
            {
                var (svcName, svcDesc) = ResolveServiceText(s);
                return new ServiceDto
                {
                    Id = s.Id,
                    CategoryId = s.CategoryId,
                    CategoryName = categoryDict.TryGetValue(s.CategoryId, out var name) ? name : string.Empty,
                    Name = svcName,
                    Description = svcDesc,
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
                };
            }).ToList();
        }

        public async Task<ServiceDto?> GetByIdAsync(int id, bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var service = await _unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
            if (service == null) return null;

            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(service.CategoryId, cancellationToken);

            if (activeOnly.HasValue && activeOnly.Value)
            {
                if (!service.IsActive || category == null || !category.IsActive)
                {
                    return null;
                }
            }

            var currentLang = _languageService.CurrentLanguage;
            var serviceTranslations = await _unitOfWork.Repository<ServiceTranslation>()
                .FindAsync(t => t.ServiceId == service.Id, cancellationToken);
            var categoryTranslations = category != null
                ? await _unitOfWork.Repository<CategoryTranslation>().FindAsync(t => t.CategoryId == category.Id, cancellationToken)
                : Array.Empty<CategoryTranslation>();

            string svcName = service.Name;
            string? svcDesc = service.Description;
            if (currentLang == "fr")
            {
                var fr = serviceTranslations.FirstOrDefault(t => t.LanguageCode == "fr");
                if (fr != null && !string.IsNullOrWhiteSpace(fr.Name))
                {
                    svcName = fr.Name;
                    svcDesc = fr.Description ?? service.Description;
                }
            }
            else
            {
                var en = serviceTranslations.FirstOrDefault(t => t.LanguageCode == "en");
                if (en != null && !string.IsNullOrWhiteSpace(en.Name))
                {
                    svcName = en.Name;
                    svcDesc = en.Description ?? service.Description;
                }
            }

            string catName = category?.Name ?? string.Empty;
            if (category != null)
            {
                if (currentLang == "fr")
                {
                    var frCat = categoryTranslations.FirstOrDefault(t => t.LanguageCode == "fr");
                    if (frCat != null && !string.IsNullOrWhiteSpace(frCat.Name)) catName = frCat.Name;
                }
                else
                {
                    var enCat = categoryTranslations.FirstOrDefault(t => t.LanguageCode == "en");
                    if (enCat != null && !string.IsNullOrWhiteSpace(enCat.Name)) catName = enCat.Name;
                }
            }

            var bookingCount = await _unitOfWork.BookingRequests.CountAsync(b => b.ServiceId == service.Id, cancellationToken);

            return new ServiceDto
            {
                Id = service.Id,
                CategoryId = service.CategoryId,
                CategoryName = catName,
                Name = svcName,
                Description = svcDesc,
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

            string serviceName = dto.Name;
            string? serviceDesc = dto.Description;

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                if (!dto.Translations.TryGetValue("en", out var enTrans) || string.IsNullOrWhiteSpace(enTrans?.Name))
                {
                    throw new InvalidOperationException(_localizationService.GetMessage("Services.EnglishRequired"));
                }
                serviceName = enTrans.Name.Trim();
                serviceDesc = enTrans.Description?.Trim();
            }
            else if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new InvalidOperationException(_localizationService.GetMessage("Services.EnglishRequired"));
            }

            var existingService = await _unitOfWork.Repository<Service>()
                .FirstOrDefaultAsync(s => s.CategoryId == dto.CategoryId && s.Name.ToLower() == serviceName.ToLower(), cancellationToken);

            if (existingService != null)
            {
                throw new InvalidOperationException($"A service with the name '{serviceName}' already exists in this category.");
            }

            var service = new Service
            {
                CategoryId = dto.CategoryId,
                Name = serviceName,
                Description = serviceDesc,
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

            // Save translations
            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                foreach (var (langCode, trans) in dto.Translations)
                {
                    if (!string.IsNullOrWhiteSpace(trans.Name))
                    {
                        await _unitOfWork.Repository<ServiceTranslation>().AddAsync(new ServiceTranslation
                        {
                            ServiceId = service.Id,
                            LanguageCode = langCode.ToLowerInvariant(),
                            Name = trans.Name.Trim(),
                            Description = trans.Description?.Trim()
                        }, cancellationToken);
                    }
                }
            }
            else
            {
                await _unitOfWork.Repository<ServiceTranslation>().AddAsync(new ServiceTranslation
                {
                    ServiceId = service.Id,
                    LanguageCode = "en",
                    Name = service.Name,
                    Description = service.Description
                }, cancellationToken);
            }
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

            string serviceName = dto.Name;
            string? serviceDesc = dto.Description;

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                if (!dto.Translations.TryGetValue("en", out var enTrans) || string.IsNullOrWhiteSpace(enTrans?.Name))
                {
                    throw new InvalidOperationException(_localizationService.GetMessage("Services.EnglishRequired"));
                }
                serviceName = enTrans.Name.Trim();
                serviceDesc = enTrans.Description?.Trim();
            }
            else if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = service.Name;
            }

            var duplicate = await _unitOfWork.Repository<Service>()
                .FirstOrDefaultAsync(s => s.Id != id && s.CategoryId == dto.CategoryId && s.Name.ToLower() == serviceName.ToLower(), cancellationToken);

            if (duplicate != null)
            {
                throw new InvalidOperationException($"Another service with the name '{serviceName}' already exists in this category.");
            }

            service.CategoryId = dto.CategoryId;
            service.Name = serviceName;
            service.Description = serviceDesc;
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

            // Update translations
            var existingTranslations = await _unitOfWork.Repository<ServiceTranslation>()
                .FindAsync(t => t.ServiceId == service.Id, cancellationToken);

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                foreach (var (code, trans) in dto.Translations)
                {
                    var existing = existingTranslations.FirstOrDefault(t => t.LanguageCode == code.ToLowerInvariant());
                    if (existing != null)
                    {
                        existing.Name = trans.Name.Trim();
                        existing.Description = trans.Description?.Trim();
                        _unitOfWork.Repository<ServiceTranslation>().Update(existing);
                    }
                    else if (!string.IsNullOrWhiteSpace(trans.Name))
                    {
                        await _unitOfWork.Repository<ServiceTranslation>().AddAsync(new ServiceTranslation
                        {
                            ServiceId = service.Id,
                            LanguageCode = code.ToLowerInvariant(),
                            Name = trans.Name.Trim(),
                            Description = trans.Description?.Trim()
                        }, cancellationToken);
                    }
                }
            }

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
