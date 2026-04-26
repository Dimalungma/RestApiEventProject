namespace RestApiEventProject.Models;

public class Booking
{
    public long Id { get; set; }

    public int EventId { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
