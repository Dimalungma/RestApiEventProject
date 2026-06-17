namespace RestApiEventProject.Models;

public class Booking
{
    private Booking()
    {
        Event = null!;
    }
    public long Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; }

    private BookingStatus _status;
    public BookingStatus Status => _status;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    //Чтобы было что тестировать
    public static Booking CreatePending(long id, int eventId)
    {
        return new Booking
        {
            Id = id,
            EventId = eventId,
            _status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };
    }

    public void Confirm()
    {
        _status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        _status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
