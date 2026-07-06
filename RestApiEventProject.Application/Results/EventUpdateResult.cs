namespace RestApiEventProject.Application;

public enum EventUpdateResult
{
    Success,
    NotFound,
    InvalidTotalSeats,
    TotalSeatsLessThanReservedSeats
}
