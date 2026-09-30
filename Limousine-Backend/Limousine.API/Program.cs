using Limousine.API;
using Limousine.API.Middleware;
using Limousine.Infrastracture.Data;
using Limousine.Infrastracture.Data.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddDbContextServices(builder.Configuration);
builder.Services.AddServices(builder.Configuration);

var app = builder.Build();

// Seed initial deployment data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await AppDbContextSeed.SeedAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}

// Global exception handling middleware
app.UseMiddleware<ExceptionMiddleware>();

// Enable Swagger UI documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Limousine API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root URL to swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
