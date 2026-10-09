using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class ServiceTranslationConfiguration : IEntityTypeConfiguration<ServiceTranslation>
    {
        public void Configure(EntityTypeBuilder<ServiceTranslation> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.LanguageCode)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            // UNIQUE constraint on (ServiceId, LanguageCode)
            builder.HasIndex(t => new { t.ServiceId, t.LanguageCode })
                .IsUnique();

            builder.HasOne(t => t.Service)
                .WithMany(s => s.Translations)
                .HasForeignKey(t => t.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
