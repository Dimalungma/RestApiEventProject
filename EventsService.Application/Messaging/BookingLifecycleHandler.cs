using EventsService.Domain;
using Microsoft.Extensions.Logging;

namespace EventsService.Application;

public sealed class BookingLifecycleHandler : IBookingLifecycleHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IBookingReservationRepository _bookingReservationRepository;
    private readonly IEventSeatEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BookingLifecycleHandler> _logger;

    public BookingLifecycleHandler(
        IEventRepository eventRepository,
        IBookingReservationRepository bookingReservationRepository,
        IEventSeatEventPublisher eventPublisher,
        ICacheService cacheService,
        ILogger<BookingLifecycleHandler> logger)
    {
        _eventRepository = eventRepository;
        _bookingReservationRepository = bookingReservationRepository;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task HandleBookingCreatedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        CancellationToken cancellationToken = default)
    {
        var existingReservation =
            await _bookingReservationRepository.GetByBookingIdAsync(
                bookingId,
                cancellationToken);

        if (existingReservation is not null)
        {
            await ReplayExistingResultAsync( //На случай падения kafka после резервации, но до возврата ответа booking, чтобы не резервировало дважды
                existingReservation,
                cancellationToken);

            return;
        }

        var eventItem = await _eventRepository.GetByIdAsync(
            eventId,
            cancellationToken);

        if (eventItem is null)
        {
            await SaveUnavailableAsync(
                bookingId,
                eventId,
                seatsCount,
                "Мероприятие не найдено",
                cancellationToken);

            return;
        }

        var reserveResult = eventItem.TryReserveSeats(seatsCount);

        if (reserveResult == ReserveSeatsResult.Success)
        {
            var reservation = BookingReservation.CreateReserved(
                bookingId,
                eventId,
                seatsCount);

            await _bookingReservationRepository.AddAsync(
                reservation,
                cancellationToken);

            await _bookingReservationRepository.SaveChangesAsync(
                cancellationToken);

            await _cacheService.RemoveAsync(
                EventCacheKeys.ById(eventId));

            await _eventPublisher.PublishEventSeatReservedAsync(
                bookingId,
                eventId,
                DateTime.UtcNow,
                cancellationToken);

            return;
        }

        var reason = reserveResult switch
        {
            ReserveSeatsResult.InvalidSeatsCount =>
                "Некорректное количество мест",

            ReserveSeatsResult.EventAlreadyStarted =>
                "Мероприятие уже началось",

            ReserveSeatsResult.NoAvailableSeats =>
                "Недостаточно свободных мест",

            _ =>
                "Место невозможно зарезервировать"
        };

        await SaveUnavailableAsync(
            bookingId,
            eventId,
            seatsCount,
            reason,
            cancellationToken);
    }

    public async Task HandleBookingCancelledAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        CancellationToken cancellationToken = default)
    {
        var reservation =
            await _bookingReservationRepository.GetByBookingIdAsync(
                bookingId,
                cancellationToken);

        if (reservation is null)
        {
            //BookingCancelled может прийти раньше BookingCreated, т.к. это разные Kafka-топики (привет гонка), но уже не сможет быть зарезервирован.
            //Я не думаю что стоит морочиться с "отменой отмены", с тем же id.
            var cancelledReservation =
                BookingReservation.CreateCancelled(
                    bookingId,
                    eventId,
                    seatsCount);

            await _bookingReservationRepository.AddAsync(
                cancelledReservation,
                cancellationToken);

            await _bookingReservationRepository.SaveChangesAsync(
                cancellationToken);

            return;
        }

        if (reservation.Status == BookingReservationStatus.Cancelled)
        {
            return;
        }
        int? changedEventId = null;

        if (reservation.Status == BookingReservationStatus.Reserved)
        {
            var eventItem = await _eventRepository.GetByIdAsync(
                reservation.EventId,
                cancellationToken);

            if (eventItem is not null)
            {
                eventItem.ReleaseSeats(reservation.SeatsCount);
                changedEventId = reservation.EventId;
            }
            else
            {
                _logger.LogWarning(
                    $"Не удалось вернуть места для брони {bookingId}: мероприятие {reservation.EventId} не найдено");
            }
        }

        reservation.Cancel();

        await _bookingReservationRepository.SaveChangesAsync(
            cancellationToken);

        if (changedEventId.HasValue)
        {
            await _cacheService.RemoveAsync(
                EventCacheKeys.ById(changedEventId.Value));
        }
    }

    private async Task SaveUnavailableAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        string reason,
        CancellationToken cancellationToken)
    {
        var reservation = BookingReservation.CreateUnavailable(
            bookingId,
            eventId,
            seatsCount,
            reason);

        await _bookingReservationRepository.AddAsync(
            reservation,
            cancellationToken);

        await _bookingReservationRepository.SaveChangesAsync(
            cancellationToken);

        await _eventPublisher.PublishEventSeatUnavailableAsync(
            bookingId,
            eventId,
            reason,
            DateTime.UtcNow,
            cancellationToken);
    }

    private async Task ReplayExistingResultAsync(
        BookingReservation reservation,
        CancellationToken cancellationToken)
    {
        switch (reservation.Status)
        {
            case BookingReservationStatus.Reserved:
                await _eventPublisher.PublishEventSeatReservedAsync(
                    reservation.BookingId,
                    reservation.EventId,
                    DateTime.UtcNow,
                    cancellationToken);

                break;

            case BookingReservationStatus.Unavailable:
                await _eventPublisher.PublishEventSeatUnavailableAsync(
                    reservation.BookingId,
                    reservation.EventId,
                    reservation.Reason ?? "Место недоступно",
                    DateTime.UtcNow,
                    cancellationToken);

                break;

            case BookingReservationStatus.Cancelled:
                //Если уже успели отменить, и сообщения тупо пришли не в том порядке
                break;
        }
    }
}