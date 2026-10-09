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
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ICurrentLanguageService _languageService;
        private readonly ILocalizationService _localizationService;

        public CategoryService(
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

        public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool? activeOnly = null, bool includeServices = false, CancellationToken cancellationToken = default)
        {
            var categories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
            var categoryTranslations = await _unitOfWork.Repository<CategoryTranslation>().GetAllAsync(cancellationToken);

            var currentLang = _languageService.CurrentLanguage;
            var catTransLookup = categoryTranslations
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            (string Name, string? Description) ResolveCategoryText(Category c)
            {
                if (catTransLookup.TryGetValue(c.Id, out var transList))
                {
                    if (currentLang == "fr")
                    {
                        var fr = transList.FirstOrDefault(t => t.LanguageCode == "fr");
                        if (fr != null && !string.IsNullOrWhiteSpace(fr.Name))
                        {
                            return (fr.Name, fr.Description ?? c.Description);
                        }
                    }
                    var en = transList.FirstOrDefault(t => t.LanguageCode == "en");
                    if (en != null && !string.IsNullOrWhiteSpace(en.Name))
                    {
                        return (en.Name, en.Description ?? c.Description);
                    }
                }
                return (c.Name, c.Description);
            }

            var query = categories.AsEnumerable();
            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(c => c.IsActive);
            }

            var sorted = query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            var services = await _unitOfWork.Repository<Service>().GetAllAsync(cancellationToken);
            var activeServicesQuery = services.AsEnumerable();
            if (activeOnly.HasValue && activeOnly.Value)
            {
                activeServicesQuery = activeServicesQuery.Where(s => s.IsActive);
            }

            var serviceCountsByCategoryId = activeServicesQuery
                .GroupBy(s => s.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            ILookup<int, ServiceDto>? servicesByCategoryLookup = null;
            if (includeServices)
            {
                servicesByCategoryLookup = activeServicesQuery
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new ServiceDto
                    {
                        Id = s.Id,
                        CategoryId = s.CategoryId,
                        CategoryName = sorted.FirstOrDefault(c => c.Id == s.CategoryId)?.Name ?? string.Empty,
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
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToLookup(s => s.CategoryId);
            }

            return sorted.Select(c =>
            {
                var (catName, catDesc) = ResolveCategoryText(c);
                return new CategoryDto
                {
                    Id = c.Id,
                    Name = catName,
                    Description = catDesc,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ServicesCount = serviceCountsByCategoryId.TryGetValue(c.Id, out var count) ? count : 0,
                    Services = includeServices && servicesByCategoryLookup != null 
                        ? servicesByCategoryLookup[c.Id].ToList() 
                        : null
                };
            }).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id, bool? activeOnly = null, bool includeServices = false, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            if (activeOnly.HasValue && activeOnly.Value && !category.IsActive)
            {
                return null;
            }

            var currentLang = _languageService.CurrentLanguage;
            var catTranslations = await _unitOfWork.Repository<CategoryTranslation>()
                .FindAsync(t => t.CategoryId == category.Id, cancellationToken);

            string catName = category.Name;
            string? catDesc = category.Description;

            if (currentLang == "fr")
            {
                var fr = catTranslations.FirstOrDefault(t => t.LanguageCode == "fr");
                if (fr != null && !string.IsNullOrWhiteSpace(fr.Name))
                {
                    catName = fr.Name;
                    catDesc = fr.Description ?? category.Description;
                }
            }
            else
            {
                var en = catTranslations.FirstOrDefault(t => t.LanguageCode == "en");
                if (en != null && !string.IsNullOrWhiteSpace(en.Name))
                {
                    catName = en.Name;
                    catDesc = en.Description ?? category.Description;
                }
            }

            var services = await _unitOfWork.Repository<Service>().GetAllAsync(cancellationToken);
            var categoryServicesQuery = services.Where(s => s.CategoryId == category.Id);
            if (activeOnly.HasValue && activeOnly.Value)
            {
                categoryServicesQuery = categoryServicesQuery.Where(s => s.IsActive);
            }

            var sortedServices = categoryServicesQuery
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    CategoryId = s.CategoryId,
                    CategoryName = catName,
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
                    UpdatedAt = s.UpdatedAt
                }).ToList();

            return new CategoryDto
            {
                Id = category.Id,
                Name = catName,
                Description = catDesc,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = sortedServices.Count,
                Services = includeServices ? sortedServices : null
            };
        }

        public async Task<IReadOnlyList<ServiceDto>> GetServicesByCategoryIdAsync(int categoryId, bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(categoryId, cancellationToken);
            if (category == null) return Array.Empty<ServiceDto>();

            if (activeOnly.HasValue && activeOnly.Value && !category.IsActive)
            {
                return Array.Empty<ServiceDto>();
            }

            var currentLang = _languageService.CurrentLanguage;
            var catTranslations = await _unitOfWork.Repository<CategoryTranslation>()
                .FindAsync(t => t.CategoryId == category.Id, cancellationToken);

            string catName = category.Name;
            if (currentLang == "fr")
            {
                var fr = catTranslations.FirstOrDefault(t => t.LanguageCode == "fr");
                if (fr != null && !string.IsNullOrWhiteSpace(fr.Name)) catName = fr.Name;
            }
            else
            {
                var en = catTranslations.FirstOrDefault(t => t.LanguageCode == "en");
                if (en != null && !string.IsNullOrWhiteSpace(en.Name)) catName = en.Name;
            }

            var services = await _unitOfWork.Repository<Service>().GetAllAsync(cancellationToken);
            var query = services.Where(s => s.CategoryId == categoryId);
            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(s => s.IsActive);
            }

            return query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    CategoryId = s.CategoryId,
                    CategoryName = catName,
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
                    UpdatedAt = s.UpdatedAt
                })
                .ToList();
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
        {
            string categoryName = dto.Name;
            string? categoryDesc = dto.Description;

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                if (!dto.Translations.TryGetValue("en", out var enTrans) || string.IsNullOrWhiteSpace(enTrans?.Name))
                {
                    throw new InvalidOperationException(_localizationService.GetMessage("Categories.EnglishRequired"));
                }
                categoryName = enTrans.Name.Trim();
                categoryDesc = enTrans.Description?.Trim();
            }
            else if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new InvalidOperationException(_localizationService.GetMessage("Categories.EnglishRequired"));
            }

            var existingCategory = await _unitOfWork.Repository<Category>()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower(), cancellationToken);

            if (existingCategory != null)
            {
                throw new InvalidOperationException($"A category with the name '{categoryName}' already exists.");
            }

            var category = new Category
            {
                Name = categoryName,
                Description = categoryDesc,
                ImageUrl = dto.ImageUrl?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _unitOfWork.Repository<Category>().AddAsync(category, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Save translations
            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                foreach (var (langCode, trans) in dto.Translations)
                {
                    if (!string.IsNullOrWhiteSpace(trans.Name))
                    {
                        await _unitOfWork.Repository<CategoryTranslation>().AddAsync(new CategoryTranslation
                        {
                            CategoryId = category.Id,
                            LanguageCode = langCode.ToLowerInvariant(),
                            Name = trans.Name.Trim(),
                            Description = trans.Description?.Trim()
                        }, cancellationToken);
                    }
                }
            }
            else
            {
                await _unitOfWork.Repository<CategoryTranslation>().AddAsync(new CategoryTranslation
                {
                    CategoryId = category.Id,
                    LanguageCode = "en",
                    Name = category.Name,
                    Description = category.Description
                }, cancellationToken);
            }
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = 0
            };
        }

        public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            string categoryName = dto.Name;
            string? categoryDesc = dto.Description;

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                if (!dto.Translations.TryGetValue("en", out var enTrans) || string.IsNullOrWhiteSpace(enTrans?.Name))
                {
                    throw new InvalidOperationException(_localizationService.GetMessage("Categories.EnglishRequired"));
                }
                categoryName = enTrans.Name.Trim();
                categoryDesc = enTrans.Description?.Trim();
            }
            else if (string.IsNullOrWhiteSpace(categoryName))
            {
                categoryName = category.Name;
            }

            var duplicate = await _unitOfWork.Repository<Category>()
                .FirstOrDefaultAsync(c => c.Id != id && c.Name.ToLower() == categoryName.ToLower(), cancellationToken);

            if (duplicate != null)
            {
                throw new InvalidOperationException($"Another category with the name '{categoryName}' already exists.");
            }

            category.Name = categoryName;
            category.Description = categoryDesc;
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                category.ImageUrl = dto.ImageUrl.Trim();
            }
            category.IsActive = dto.IsActive;
            category.DisplayOrder = dto.DisplayOrder;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Category>().Update(category);

            // Update translations
            var existingTranslations = await _unitOfWork.Repository<CategoryTranslation>()
                .FindAsync(t => t.CategoryId == category.Id, cancellationToken);

            if (dto.Translations != null && dto.Translations.Count > 0)
            {
                foreach (var (code, trans) in dto.Translations)
                {
                    var existing = existingTranslations.FirstOrDefault(t => t.LanguageCode == code.ToLowerInvariant());
                    if (existing != null)
                    {
                        existing.Name = trans.Name.Trim();
                        existing.Description = trans.Description?.Trim();
                        _unitOfWork.Repository<CategoryTranslation>().Update(existing);
                    }
                    else if (!string.IsNullOrWhiteSpace(trans.Name))
                    {
                        await _unitOfWork.Repository<CategoryTranslation>().AddAsync(new CategoryTranslation
                        {
                            CategoryId = category.Id,
                            LanguageCode = code.ToLowerInvariant(),
                            Name = trans.Name.Trim(),
                            Description = trans.Description?.Trim()
                        }, cancellationToken);
                    }
                }
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var serviceCount = await _unitOfWork.Repository<Service>()
                .CountAsync(s => s.CategoryId == category.Id, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = serviceCount
            };
        }

        public async Task<CategoryDto?> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            category.IsActive = isActive;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var serviceCount = await _unitOfWork.Repository<Service>()
                .CountAsync(s => s.CategoryId == category.Id, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = serviceCount
            };
        }

        public async Task<CategoryDto?> UpdateDisplayOrderAsync(int id, int displayOrder, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            category.DisplayOrder = displayOrder;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var serviceCount = await _unitOfWork.Repository<Service>()
                .CountAsync(s => s.CategoryId == category.Id, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = serviceCount
            };
        }

        public async Task<CategoryDto?> UploadImageAsync(int id, Stream imageStream, string fileName, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return null;

            var oldImageUrl = category.ImageUrl;

            var newImageUrl = await _fileService.SaveFileAsync(imageStream, fileName, "categories", cancellationToken);
            category.ImageUrl = newImageUrl;
            category.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Clean up old uploaded file if it was in the uploads directory
            if (!string.IsNullOrWhiteSpace(oldImageUrl) && oldImageUrl.StartsWith("/uploads/categories/"))
            {
                _fileService.DeleteFile(oldImageUrl);
            }

            var serviceCount = await _unitOfWork.Repository<Service>()
                .CountAsync(s => s.CategoryId == category.Id, cancellationToken);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServicesCount = serviceCount
            };
        }

        public async Task<bool> DeleteAsync(int id, bool archiveOnly = true, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
            if (category == null) return false;

            if (archiveOnly)
            {
                // Soft delete / archive: Preserves existing services and historical bookings automatically
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Category>().Update(category);
                await _unitOfWork.CompleteAsync(cancellationToken);
                return true;
            }

            // Hard delete request: Enforce business rule to protect existing services
            var hasServices = await _unitOfWork.Repository<Service>()
                .AnyAsync(s => s.CategoryId == id, cancellationToken);

            if (hasServices)
            {
                throw new InvalidOperationException("Cannot permanently delete category with associated services. Archive or deactivate the category instead.");
            }

            _unitOfWork.Repository<Category>().Delete(category);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }
    }
}
