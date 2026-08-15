using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingsService.Domain;

namespace BookingsService.Infrastructure.DataAccess;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .UseIdentityByDefaultColumn();

        builder.Property(b => b.EventId)
            .IsRequired();

        builder.Property(booking => booking.UserId)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasField("_status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.ProcessedAt);
    }
}