namespace EventsService.Application;

public enum EventUpdateResult
{
    Success,
    NotFound,
    InvalidTotalSeats,
    TotalSeatsLessThanReservedSeats
}
