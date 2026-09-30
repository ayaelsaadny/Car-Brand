using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Limousine.Infrastracture.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        #region DbSets
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleImage> VehicleImages { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Media> MediaFiles { get; set; }
        public DbSet<WebsiteSetting> WebsiteSettings { get; set; }
        #endregion
    }
}
