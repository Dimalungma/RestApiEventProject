using Microsoft.Extensions.Logging;

namespace RestApiEventProject.Application;

public class BookingProcessingService : IBookingProcessingService
{
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessingService> _logger;

    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingProcessingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository,
        ILogger<BookingProcessingService> logger)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.GetPendingBookingIdsAsync(cancellationToken);
    }

    public async Task ProcessBookingAsync(long bookingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

            if (booking is null)
            {
                _logger.LogWarning($"Бронь с id {bookingId} не найдена");

                return;
            }

            _logger.LogInformation($"Начата фоновая обработка брони с id {booking.Id} для мероприятия с id {booking.EventId}");

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Бронь с id {booking.Id} для мероприятия с id {booking.EventId} убрана из фоновой обработки, прислали CancellationToken");

                return;
            }

            await Task.Delay(ProcessingDelay, cancellationToken); //Имитируем тяжелую операцию, а-ля бронь оплачивается и оформляется

            if (Random.Shared.Next(0, 6) == 5) //Плюс рандом mock отклонения заказа
            {
                throw new PaymentRejectedException($"Бронь {booking.Id} отменена, не прошла оплата (Random)");
            }

            var existingEvent = await _eventRepository.GetByIdAsync(booking.EventId, cancellationToken);

            if (existingEvent is null)
            {
                _logger.LogWarning($"Мероприятие с id {booking.EventId} не найдено, отменяю бронирование с id {booking.Id}");

                booking.Reject();

                await _bookingRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"Бронь с id {booking.Id} отменена");

                return;
            }

            booking.Confirm();

            await _bookingRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (PaymentRejectedException exception)
        {
            _logger.LogError(exception, $"Ошибка при оплате брони с id {bookingId}, отменяю бронирование");

            await RejectBookingAndReleaseSeatAsync(bookingId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {bookingId}, отменяю бронирование");

            await RejectBookingAndReleaseSeatAsync(bookingId, cancellationToken);
        }
    }

    private async Task RejectBookingAndReleaseSeatAsync(long bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

            if (booking is null)
            {
                return;
            }

            var existingEvent = await _eventRepository.GetByIdAsync(booking.EventId, cancellationToken);

            if (existingEvent is not null)
            {
                existingEvent.ReleaseSeats();
            }

            booking.Reject();

            await _bookingRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Бронь с id {booking.Id} отменена");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Не удалось отменить бронь с id {bookingId} после ошибки фоновой обработки");
        }
    }

    private sealed class PaymentRejectedException : Exception
    {
        public PaymentRejectedException(string message) : base(message)
        {
        }
    }
}