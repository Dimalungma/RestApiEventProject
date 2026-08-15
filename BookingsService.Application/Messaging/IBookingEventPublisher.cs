namespace BookingsService.Application;

public interface IBookingEventPublisher
{
    Task PublishBookingCreatedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishBookingConfirmedAsync(
        long bookingId,
        int eventId,
        long userId,
        int seatsCount,
        DateTime confirmedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishBookingRejectedAsync(
        long bookingId,
        int eventId,
        long userId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishBookingCancelledAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default);
}