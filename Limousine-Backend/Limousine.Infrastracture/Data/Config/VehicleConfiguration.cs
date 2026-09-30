using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.Make)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.Model)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.LicensePlate)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(v => v.LicensePlate)
                .IsUnique();

            builder.Property(v => v.Color)
                .HasMaxLength(50);

            builder.Property(v => v.PricePerHour)
                .HasPrecision(18, 2);

            builder.Property(v => v.PricePerKm)
                .HasPrecision(18, 2);

            builder.Property(v => v.Features)
                .HasMaxLength(1000);

            builder.Property(v => v.Description)
                .HasMaxLength(1000);

            builder.Property(v => v.IsActive)
                .HasDefaultValue(true);

            builder.Property(v => v.IsDefault)
                .HasDefaultValue(false);

            builder.Property(v => v.UpdatedAt)
                .IsRequired(false);

            builder.HasMany(v => v.Images)
                .WithOne(img => img.Vehicle)
                .HasForeignKey(img => img.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
