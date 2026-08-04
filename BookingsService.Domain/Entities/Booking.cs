namespace BookingsService.Domain;

public class Booking
{
    private Booking()
    {
        Event = null!;
        User = null!;
    }
    public long Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }

    private BookingStatus _status;
    public BookingStatus Status => _status;

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    //Create для упрощения инициализации и тестирования
    public static Booking CreatePending( int eventId, long userId)
    {
        return new Booking
        {
            EventId = eventId,
            UserId = userId,
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

    public bool Cancel()
    {
        if (_status == BookingStatus.Cancelled) //От повторных перезаписей и освобождения лишних мест
        {
            return false;
        }

        _status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;

        return true;
    }
}
