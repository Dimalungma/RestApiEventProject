namespace EventsService.Domain;

public enum ReserveSeatsResult
{
    Success,
    InvalidSeatsCount,
    EventAlreadyStarted,
    NoAvailableSeats
}