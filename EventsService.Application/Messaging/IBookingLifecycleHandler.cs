namespace EventsService.Application;

public interface IBookingLifecycleHandler
{
    Task HandleBookingCreatedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        CancellationToken cancellationToken = default);

    Task HandleBookingCancelledAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        CancellationToken cancellationToken = default);
}