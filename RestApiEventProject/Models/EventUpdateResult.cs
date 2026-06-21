namespace RestApiEventProject.Models;

public enum EventUpdateResult
{
    Success,
    NotFound,
    TotalSeatsLessThanReserved
}
