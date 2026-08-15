namespace EventsService.Application;

public interface IEventSeatEventPublisher
{
    Task PublishEventSeatReservedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishEventSeatUnavailableAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default);
}