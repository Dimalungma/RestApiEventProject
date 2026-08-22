using BookingsService.Domain;
using Microsoft.Extensions.Logging;

namespace BookingsService.Application;

public sealed class EventSeatResultHandler : IEventSeatResultHandler
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingEventPublisher _bookingEventPublisher;
    private readonly ILogger<EventSeatResultHandler> _logger;

    public EventSeatResultHandler(
        IBookingRepository bookingRepository,
        IBookingEventPublisher bookingEventPublisher,
        ILogger<EventSeatResultHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _bookingEventPublisher = bookingEventPublisher;
        _logger = logger;
    }

    public async Task HandleSeatReservedAsync(
        long bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(
            bookingId,
            cancellationToken);

        if (booking is null)
        {
            _logger.LogWarning(
                $"Бронь с id {bookingId} не найдена при обработке EventSeatReserved");

            return;
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            //Отмена могла пересечься с резервированием,
            //так что повторно сообщаем Events, что место надо освободить.
            await _bookingEventPublisher.PublishBookingCancelledAsync(
                booking.Id,
                booking.EventId,
                BookingConstants.SeatsPerBooking,
                booking.ProcessedAt ?? DateTime.UtcNow,
                cancellationToken);

            return;
        }

        if (booking.Status == BookingStatus.Confirmed)
        {
            //Если статус сохранился, а BookingConfirmed потеряли.
            await PublishConfirmedAsync(
                booking,
                cancellationToken);

            return;
        }

        if (booking.Status != BookingStatus.AwaitingConfirmation)
        {
            _logger.LogWarning(
                $"Бронь {booking.Id} получила EventSeatReserved при том, что статус {booking.Status}");

            return;
        }

        if (!booking.TryConfirm())
        {
            return;
        }

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        await PublishConfirmedAsync(
            booking,
            cancellationToken);
    }

    public async Task HandleSeatUnavailableAsync(
        long bookingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(
            bookingId,
            cancellationToken);

        if (booking is null)
        {
            _logger.LogWarning(
                $"Бронь с id {bookingId} не найдена при обработке EventSeatUnavailable");

            return;
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return;
        }

        if (booking.Status == BookingStatus.Rejected)
        {
            //Предыдущая публикация BookingRejected могла не завершиться.
            await PublishRejectedAsync(
                booking,
                reason,
                cancellationToken);

            return;
        }

        if (booking.Status != BookingStatus.AwaitingConfirmation)
        {
            _logger.LogWarning(
                $"Бронь {booking.Id} получила EventSeatUnavailable при том, что статус {booking.Status}");

            return;
        }

        if (!booking.TryReject())
        {
            return;
        }

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        await PublishRejectedAsync(
            booking,
            reason,
            cancellationToken);
    }

    private Task PublishConfirmedAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        return _bookingEventPublisher.PublishBookingConfirmedAsync(
            booking.Id,
            booking.EventId,
            booking.UserId,
            BookingConstants.SeatsPerBooking,
            booking.ProcessedAt ?? DateTime.UtcNow,
            cancellationToken);
    }

    private Task PublishRejectedAsync(
        Booking booking,
        string reason,
        CancellationToken cancellationToken)
    {
        return _bookingEventPublisher.PublishBookingRejectedAsync(
            booking.Id,
            booking.EventId,
            booking.UserId,
            reason,
            booking.ProcessedAt ?? DateTime.UtcNow,
            cancellationToken);
    }
}