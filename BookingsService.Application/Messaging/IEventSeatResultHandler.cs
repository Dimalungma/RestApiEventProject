namespace BookingsService.Application;

public interface IEventSeatResultHandler
{
    Task HandleSeatReservedAsync(
        long bookingId,
        CancellationToken cancellationToken = default);

    Task HandleSeatUnavailableAsync(
        long bookingId,
        string reason,
        CancellationToken cancellationToken = default);
}