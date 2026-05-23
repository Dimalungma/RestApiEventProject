using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApiEventProject.Models;

namespace RestApiEventProject.DataAccess.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever(); //Учитывая что я отказался от guid, возможно стоит дать автогенерацию

        builder.Property(b => b.EventId)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasField("_status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.ProcessedAt);

        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId);
    }
}