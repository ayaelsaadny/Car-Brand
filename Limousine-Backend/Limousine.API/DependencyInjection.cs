using Limousine.API.Errors;
using Limousine.API.Services;
using Limousine.Application.Interfaces;
using Limousine.Application.Services;
using Limousine.Core.Interfaces.Repositories;
using Limousine.Infrastracture.Data;
using Limousine.Infrastracture.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Text;

namespace Limousine.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDbContextServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // Repositories & Unit of Work
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IVehicleRepository, VehicleRepository>();
            services.AddScoped<IBookingRequestRepository, BookingRequestRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Application Services
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IFileService, FileService>();

            // Standardize Model Validation Errors to ApiValidationErrorResponse
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToArray();

                    var errorResponse = new ApiValidationErrorResponse(errors);
                    return new BadRequestObjectResult(errorResponse);
                };
            });

            // Authentication & JWT Bearer
            var jwtKey = configuration?["Jwt:Key"] ?? "LimousineSecretKeyForJwtAuthenticationWhichIsVeryLongAndSecure12345";
            var jwtIssuer = configuration?["Jwt:Issuer"] ?? "LimousineAPI";
            var jwtAudience = configuration?["Jwt:Audience"] ?? "LimousineClient";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true
                };
            });

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "Limousine API",
                    Version = "v1",
                    Description = "Limousine Backend Web API - Clean Architecture"
                });

                var securityScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and your token.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    }
                });
            });
            return services;
        }
    }
}
