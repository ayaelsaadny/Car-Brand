using Limousine.Core.Entities;
using Limousine.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Limousine.Infrastracture.Data.Seed
{
    public static class AppDbContextSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // 1. Seed Admin Account (admin@admin.com / admin123)
                var adminEmail = "admin@admin.com";
                var adminUser = await context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser == null)
                {
                    logger.LogInformation("Seeding default Admin account (admin@admin.com)...");
                    adminUser = new ApplicationUser
                    {
                        FullName = "System Administrator",
                        Email = adminEmail,
                        PasswordHash = HashPassword("admin123"),
                        PhoneNumber = "+1234567890",
                        Role = UserRole.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.ApplicationUsers.AddAsync(adminUser);
                    await context.SaveChangesAsync();

                    var adminCustomer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == adminUser.Id);
                    if (adminCustomer == null)
                    {
                        adminCustomer = new Customer
                        {
                            UserId = adminUser.Id,
                            FirstName = "System",
                            LastName = "Administrator",
                            Email = adminUser.Email,
                            PhoneNumber = adminUser.PhoneNumber ?? "+1234567890",
                            Address = "Headquarters",
                            CreatedAt = DateTime.UtcNow
                        };

                        await context.Customers.AddAsync(adminCustomer);
                        await context.SaveChangesAsync();
                    }
                }

                // 1b. Seed Employee / Dispatcher Account (employee@limousine.com / employee123)
                var employeeEmail = "employee@limousine.com";
                var employeeUser = await context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == employeeEmail);
                if (employeeUser == null)
                {
                    logger.LogInformation("Seeding default Employee account (employee@limousine.com)...");
                    employeeUser = new ApplicationUser
                    {
                        FullName = "Operations Dispatcher",
                        Email = employeeEmail,
                        PasswordHash = HashPassword("employee123"),
                        PhoneNumber = "+1987654321",
                        Role = UserRole.Employee,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.ApplicationUsers.AddAsync(employeeUser);
                    await context.SaveChangesAsync();

                    var empCustomer = new Customer
                    {
                        UserId = employeeUser.Id,
                        FirstName = "Operations",
                        LastName = "Dispatcher",
                        Email = employeeUser.Email,
                        PhoneNumber = employeeUser.PhoneNumber ?? "+1987654321",
                        Address = "Fleet Dispatch Desk",
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Customers.AddAsync(empCustomer);
                    await context.SaveChangesAsync();
                }

                // 2. Seed One or More Active Categories
                if (!await context.Categories.AnyAsync())
                {
                    logger.LogInformation("Seeding initial Categories...");
                    var categories = new List<Category>
                    {
                        new Category
                        {
                            Name = "Airport Transfer",
                            Description = "Punctual, stress-free transfers to and from international and private airports.",
                            ImageUrl = "/images/categories/airport-transfer.jpg",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Category
                        {
                            Name = "Corporate Travel",
                            Description = "Executive transportation for business meetings, conferences, and roadshows.",
                            ImageUrl = "/images/categories/corporate-travel.jpg",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Category
                        {
                            Name = "Special Events & VIP",
                            Description = "Luxury chauffeur service for galas, weddings, and exclusive private celebrations.",
                            ImageUrl = "/images/categories/special-events.jpg",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Categories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();
                }

                // 3. Seed One or More Active Services
                if (!await context.Services.AnyAsync())
                {
                    logger.LogInformation("Seeding initial Services...");
                    var airportCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Airport Transfer");
                    var corporateCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Corporate Travel");

                    var services = new List<Service>();

                    if (airportCategory != null)
                    {
                        services.Add(new Service
                        {
                            Name = "VIP Airport Meet & Greet",
                            Description = "Premium curb-side or gate meet-and-greet with flight tracking and luggage assistance.",
                            ImageUrl = "/images/services/airport-vip.jpg",
                            Price = 150.00m,
                            IsActive = true,
                            CategoryId = airportCategory.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (corporateCategory != null)
                    {
                        services.Add(new Service
                        {
                            Name = "Executive Hourly Chauffeur",
                            Description = "Dedicated executive chauffeur by the hour for seamless point-to-point business travel.",
                            ImageUrl = "/images/services/hourly-chauffeur.jpg",
                            Price = 120.00m,
                            IsActive = true,
                            CategoryId = corporateCategory.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (services.Count > 0)
                    {
                        await context.Services.AddRangeAsync(services);
                        await context.SaveChangesAsync();
                    }
                }

                // 4. Seed One Active Default Vehicle
                if (!await context.Vehicles.AnyAsync())
                {
                    logger.LogInformation("Seeding initial active Default Vehicle...");
                    var defaultVehicle = new Vehicle
                    {
                        Name = "Mercedes-Benz S-Class",
                        Make = "Mercedes-Benz",
                        Model = "S 580",
                        Year = 2024,
                        LicensePlate = "LIM-7001",
                        Color = "Obsidian Black",
                        PassengerCapacity = 4,
                        LuggageCapacity = 3,
                        PricePerHour = 150.00m,
                        PricePerKm = 3.50m,
                        Features = "Burmester 3D Surround Sound, Rear-Seat Executive Package, Panoramic Sunroof, Privacy Glass, High-Speed Wi-Fi, Chilled Refreshments",
                        IsActive = true,
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow,
                        Images = new List<VehicleImage>
                        {
                            new VehicleImage
                            {
                                ImageUrl = "/images/vehicles/mercedes-s-class-main.jpg",
                                IsPrimary = true,
                                DisplayOrder = 1,
                                CreatedAt = DateTime.UtcNow
                            },
                            new VehicleImage
                            {
                                ImageUrl = "/images/vehicles/mercedes-s-class-interior.jpg",
                                IsPrimary = false,
                                DisplayOrder = 2,
                                CreatedAt = DateTime.UtcNow
                            }
                        }
                    };

                    await context.Vehicles.AddAsync(defaultVehicle);
                    await context.SaveChangesAsync();
                }

                // 5. Seed Website Settings
                if (!await context.WebsiteSettings.AnyAsync())
                {
                    logger.LogInformation("Seeding initial Website Settings...");
                    var defaultVehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.IsDefault);

                    var settings = new List<WebsiteSetting>
                    {
                        new WebsiteSetting { Key = "CompanyName", Value = "Premier Limousine Luxury Fleet", Description = "Company business title", Group = "General" },
                        new WebsiteSetting { Key = "ContactEmail", Value = "support@limousine.com", Description = "Primary support email address", Group = "Contact" },
                        new WebsiteSetting { Key = "ContactPhone", Value = "+1 (800) 555-0199", Description = "24/7 Dispatch phone line", Group = "Contact" },
                        new WebsiteSetting { Key = "Currency", Value = "USD", Description = "Base billing currency", Group = "Billing" },
                        new WebsiteSetting { Key = "DefaultVehicleId", Value = defaultVehicle?.Id.ToString() ?? "1", Description = "System default vehicle ID for single-vehicle operations", Group = "Booking" }
                    };

                    await context.WebsiteSettings.AddRangeAsync(settings);
                    await context.SaveChangesAsync();
                }

                // 5. Seed Category & Service Translations (en & fr)
                var allCategories = await context.Categories.ToListAsync();
                foreach (var cat in allCategories)
                {
                    if (!await context.CategoryTranslations.AnyAsync(t => t.CategoryId == cat.Id && t.LanguageCode == "en"))
                    {
                        await context.CategoryTranslations.AddAsync(new CategoryTranslation
                        {
                            CategoryId = cat.Id,
                            LanguageCode = "en",
                            Name = cat.Name,
                            Description = cat.Description
                        });
                    }

                    if (!await context.CategoryTranslations.AnyAsync(t => t.CategoryId == cat.Id && t.LanguageCode == "fr"))
                    {
                        var frName = cat.Name switch
                        {
                            "Airport Transfer" => "Transfert Aéroport",
                            "Corporate Travel" => "Voyages d'Affaires",
                            "Special Events & VIP" => "Événements Spéciaux et VIP",
                            "Corporate Transportation" => "Transport Corporatif",
                            _ => cat.Name
                        };
                        var frDesc = cat.Description switch
                        {
                            "Punctual, stress-free transfers to and from international and private airports." => "Transferts ponctuels et sereins depuis et vers les aéroports internationaux et privés.",
                            "Executive transportation for business meetings, conferences, and roadshows." => "Transport exécutif pour réunions d'affaires, conférences et tournées professionnelles.",
                            "Luxury chauffeur service for galas, weddings, and exclusive private celebrations." => "Service de chauffeur de luxe pour galas, mariages et réceptions privées exclusives.",
                            _ => cat.Description
                        };

                        await context.CategoryTranslations.AddAsync(new CategoryTranslation
                        {
                            CategoryId = cat.Id,
                            LanguageCode = "fr",
                            Name = frName,
                            Description = frDesc
                        });
                    }
                }
                await context.SaveChangesAsync();

                var allServices = await context.Services.ToListAsync();
                foreach (var srv in allServices)
                {
                    if (!await context.ServiceTranslations.AnyAsync(t => t.ServiceId == srv.Id && t.LanguageCode == "en"))
                    {
                        await context.ServiceTranslations.AddAsync(new ServiceTranslation
                        {
                            ServiceId = srv.Id,
                            LanguageCode = "en",
                            Name = srv.Name,
                            Description = srv.Description
                        });
                    }

                    if (!await context.ServiceTranslations.AnyAsync(t => t.ServiceId == srv.Id && t.LanguageCode == "fr"))
                    {
                        var frName = srv.Name switch
                        {
                            "VIP Airport Meet & Greet" => "Accueil VIP à l'Aéroport",
                            "Executive Hourly Chauffeur" => "Chauffeur Exécutif à l'Heure",
                            "Executive Boardroom Chauffeur" => "Chauffeur de Direction et Conseil",
                            _ => srv.Name
                        };
                        var frDesc = srv.Description switch
                        {
                            "Premium curb-side or gate meet-and-greet with flight tracking and luggage assistance." => "Accueil personnalisé au terminal ou en porte avec suivi des vols et prise en charge des bagages.",
                            "Dedicated executive chauffeur by the hour for seamless point-to-point business travel." => "Service de chauffeur privé dédié à l'heure pour vos déplacements professionnels sans interruption.",
                            "Dedicated discreet chauffeur service for VIP executives and corporate delegations." => "Service de chauffeur discret et haut de gamme pour cadres dirigeants et délégations d'affaires.",
                            _ => srv.Description
                        };

                        await context.ServiceTranslations.AddAsync(new ServiceTranslation
                        {
                            ServiceId = srv.Id,
                            LanguageCode = "fr",
                            Name = frName,
                            Description = frDesc
                        });
                    }
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding initial data to the database.");
                throw;
            }
        }

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);
            byte[] checkHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);
            return CryptographicOperations.FixedTimeEquals(hash, checkHash);
        }
    }
}
