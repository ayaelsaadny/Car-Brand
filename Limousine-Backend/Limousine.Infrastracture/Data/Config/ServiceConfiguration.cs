using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(s => s.Price)
                .HasPrecision(18, 2);

            builder.Property(s => s.FromLocation)
                .HasMaxLength(250);

            builder.Property(s => s.ToLocation)
                .HasMaxLength(250);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.Property(s => s.AllowVehicleSelection)
                .HasDefaultValue(true);

            builder.Property(s => s.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(s => s.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
