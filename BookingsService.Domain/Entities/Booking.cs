namespace BookingsService.Domain;

public class Booking
{
    private Booking()
    {
    }
    public long Id { get; set; }

    public int EventId { get; set; }

    public long UserId { get; set; }
    public DateTime? ConfirmationRequestedAt { get; private set; }


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
            ProcessedAt = null,
            ConfirmationRequestedAt = null
        };
    }

    public bool TryStartConfirmation()
    {
        if (_status != BookingStatus.Pending)
        {
            return false;
        }

        _status = BookingStatus.AwaitingConfirmation;

        return true;
    }

    public bool TryConfirm()
    {
        if (_status != BookingStatus.AwaitingConfirmation)
        {
            return false;
        }

        _status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;

        return true;
    }

    public bool TryReject()
    {
        if (_status != BookingStatus.Pending &&
            _status != BookingStatus.AwaitingConfirmation)
        {
            return false;
        }

        _status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;

        return true;
    }

    public bool Cancel()
    {
        if (_status == BookingStatus.Cancelled ||
            _status == BookingStatus.Rejected)
        {
            return false;
        }

        _status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;

        return true;
    }

    public bool MarkConfirmationRequested()
    {
        if (_status != BookingStatus.AwaitingConfirmation ||
            ConfirmationRequestedAt is not null)
        {
            return false;
        }

        ConfirmationRequestedAt = DateTime.UtcNow;

        return true;
    }
}
