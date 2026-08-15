namespace BookingsService.Domain;

public enum BookingStatus
{
    Pending,
    AwaitingConfirmation,
    Confirmed,
    Rejected,
    Cancelled
}
