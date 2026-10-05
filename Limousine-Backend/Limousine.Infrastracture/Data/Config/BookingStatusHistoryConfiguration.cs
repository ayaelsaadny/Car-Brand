using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
    {
        public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.PreviousStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(h => h.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(h => h.ChangedBy)
                .HasMaxLength(150);

            builder.Property(h => h.Notes)
                .HasMaxLength(500);

            // Ignore alias properties
            builder.Ignore(h => h.BookingId);
            builder.Ignore(h => h.OldStatus);
            builder.Ignore(h => h.CreatedAt);
            builder.Ignore(h => h.Reason);
        }
    }
}
