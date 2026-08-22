namespace EventsService.Application;

public interface IEventSeatEventPublisher
{
    Task PublishEventSeatReservedAsync(
        long bookingId,
        int eventId, //Все таки нужен для правильного Partition
        DateTime reservedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishEventSeatUnavailableAsync(
        long bookingId,
        int eventId, //Все таки нужен для правильного Partition
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default);
}