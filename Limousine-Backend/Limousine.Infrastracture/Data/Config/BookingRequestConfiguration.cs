using Limousine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Limousine.Infrastracture.Data.Config
{
    public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
    {
        public void Configure(EntityTypeBuilder<BookingRequest> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.BookingReference)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(b => b.BookingReference)
                .IsUnique();

            // Preserved historical customer snapshot fields
            builder.Property(b => b.CustomerFullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(b => b.CustomerEmail)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(b => b.CustomerPhoneNumber)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(b => b.PickupLocation)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.DropoffLocation)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(b => b.FlightNumber)
                .HasMaxLength(50);

            builder.Property(b => b.SpecialNotes)
                .HasMaxLength(1000);

            builder.Property(b => b.EstimatedPrice)
                .HasPrecision(18, 2);

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.HasOne(b => b.Customer)
                .WithMany(c => c.BookingRequests)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Service)
                .WithMany(s => s.BookingRequests)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Vehicle)
                .WithMany(v => v.BookingRequests)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.StatusHistories)
                .WithOne(h => h.BookingRequest)
                .HasForeignKey(h => h.BookingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
