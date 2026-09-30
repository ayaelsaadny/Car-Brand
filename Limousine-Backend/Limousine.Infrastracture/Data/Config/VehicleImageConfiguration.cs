using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImage>
    {
        public void Configure(EntityTypeBuilder<VehicleImage> builder)
        {
            builder.HasKey(img => img.Id);

            builder.Property(img => img.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(img => img.IsPrimary)
                .HasDefaultValue(false);

            builder.Ignore(img => img.IsMain);

            builder.Property(img => img.DisplayOrder)
                .HasDefaultValue(0);

            builder.HasIndex(img => new { img.VehicleId, img.IsPrimary });
        }
    }
}
