namespace EventsService.Domain;

public class BookingReservation
{
    private BookingReservation()
    {
    }

    public long BookingId { get; set; }

    public int EventId { get; set; }

    public int SeatsCount { get; set; }

    private BookingReservationStatus _status;
    public BookingReservationStatus Status => _status;

    public string? Reason { get; set; }

    public static BookingReservation CreateReserved(long bookingId, int eventId, int seatsCount)
    {
        return new BookingReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            SeatsCount = seatsCount,
            _status = BookingReservationStatus.Reserved
        };
    }

    public static BookingReservation CreateUnavailable(long bookingId, int eventId, int seatsCount, string reason)
    {
        return new BookingReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            SeatsCount = seatsCount,
            _status = BookingReservationStatus.Unavailable,
            Reason = reason
        };
    }

    public static BookingReservation CreateCancelled(long bookingId, int eventId, int seatsCount)
    {
        return new BookingReservation
        {
            BookingId = bookingId,
            EventId = eventId,
            SeatsCount = seatsCount,
            _status = BookingReservationStatus.Cancelled
        };
    }

    public bool Cancel()
    {
        if (_status == BookingReservationStatus.Cancelled)
        {
            return false;
        }

        _status = BookingReservationStatus.Cancelled;

        return true;
    }
}