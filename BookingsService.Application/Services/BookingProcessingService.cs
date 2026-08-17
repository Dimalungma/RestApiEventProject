using BookingsService.Domain;
using Microsoft.Extensions.Logging;

namespace BookingsService.Application;

public class BookingProcessingService : IBookingProcessingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingEventPublisher _bookingEventPublisher;
    private readonly ILogger<BookingProcessingService> _logger;

    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingProcessingService(
        IBookingRepository bookingRepository, 
        IBookingEventPublisher bookingEventPublisher,
        ILogger<BookingProcessingService> logger)
    {
        _bookingRepository = bookingRepository; 
        _bookingEventPublisher = bookingEventPublisher;
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

            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogInformation($"Бронь с id {booking.Id} уже не находится в статусе Pending");

                return;
            }

            _logger.LogInformation($"Начата фоновая обработка брони с id {booking.Id} для мероприятия с id {booking.EventId}");

            await Task.Delay(ProcessingDelay, cancellationToken); //Имитируем тяжелую операцию, а-ля бронь оплачивается и оформляется

            if (Random.Shared.Next(0, 6) == 5) //Плюс рандом mock отклонения заказа
            {
                throw new PaymentRejectedException($"Бронь {booking.Id} отклонена, не прошла оплата (Random)");
            }

            if (!booking.TryStartConfirmation())
            {
                _logger.LogInformation($"Бронь с id {booking.Id} не удалось перевести в ожидание подтверждения");

                return;
            }

            await _bookingRepository.SaveChangesAsync(cancellationToken);

            await _bookingEventPublisher.PublishBookingCreatedAsync(
                booking.Id,
                booking.EventId,
                BookingConstants.SeatsPerBooking,
                booking.CreatedAt,
                cancellationToken);

            _logger.LogInformation($"Для брони с id {booking.Id} опубликовано событие BookingCreated");
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (PaymentRejectedException exception)
        {
            _logger.LogError(exception, $"Ошибка при оплате брони с id {bookingId}, отклоняю бронирование");

            await RejectBookingAsync(bookingId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {bookingId}");
        }
    }

    private async Task RejectBookingAsync(long bookingId, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

            if (booking is null)
            {
                return;
            }

            if (!booking.TryReject())
            {
                return;
            }

            await _bookingRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Бронь с id {booking.Id} отклонена");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Не удалось отклонить бронь с id {bookingId} после ошибки фоновой обработки");
        }
    }

    private sealed class PaymentRejectedException : Exception
    {
        public PaymentRejectedException(string message) : base(message)
        {
        }
    }
}