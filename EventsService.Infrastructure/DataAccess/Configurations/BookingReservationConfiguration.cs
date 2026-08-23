using EventsService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventsService.Infrastructure.DataAccess;

internal sealed class BookingReservationConfiguration : IEntityTypeConfiguration<BookingReservation>
{
    public void Configure(EntityTypeBuilder<BookingReservation> builder)
    {
        builder.ToTable("BookingReservations");

        builder.HasKey(reservation => reservation.BookingId);

        builder.Property(reservation => reservation.BookingId)
            .ValueGeneratedNever();

        builder.Property(reservation => reservation.EventId)
            .IsRequired();

        builder.Property(reservation => reservation.SeatsCount)
            .IsRequired();

        builder.Property(reservation => reservation.Status)
            .HasField("_status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(reservation => reservation.Reason)
            .HasMaxLength(500);
    }
}