using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
    {
        public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
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

            // UNIQUE constraint on (CategoryId, LanguageCode)
            builder.HasIndex(t => new { t.CategoryId, t.LanguageCode })
                .IsUnique();

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Translations)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
