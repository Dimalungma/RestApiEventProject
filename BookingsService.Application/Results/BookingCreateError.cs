namespace BookingsService.Application;
public enum BookingCreateError
{
    EventNotFound,
    NoAvailableSeats,
    EventAlreadyStarted, //Опять-таки, я против использования исключений для бизнес логики, поэтому идем через error'ы
    ActiveBookingsLimitExceeded
}
