using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class WebsiteSettingConfiguration : IEntityTypeConfiguration<WebsiteSetting>
    {
        public void Configure(EntityTypeBuilder<WebsiteSetting> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(s => s.Key)
                .IsUnique();

            builder.Property(s => s.Value)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasMaxLength(255);

            builder.Property(s => s.Group)
                .HasMaxLength(50)
                .HasDefaultValue("General");
        }
    }
}
