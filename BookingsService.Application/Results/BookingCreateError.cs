namespace BookingsService.Application;

public enum BookingCreateError
{
    ActiveBookingsLimitExceeded //По сути нам теперь enum и не нужен, но оставлю для соответствия прошлому коду, и возможно для будущих бизнес-ошибок
}