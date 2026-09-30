using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(m => m.FileUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(m => m.FileType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Title)
                .HasMaxLength(150);

            builder.Property(m => m.AltText)
                .HasMaxLength(255);
        }
    }
}
