namespace EventsService.Application;

public interface IEventSeatEventPublisher
{
    Task PublishEventSeatReservedAsync(
        long bookingId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishEventSeatUnavailableAsync(
        long bookingId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default);
}