using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IBookingProcessingService _bookingProcessingService;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly IEventService _eventService;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5); 
    //Увеличил до 5 в рамках отладки, так как нереально отловить момент смены, сразу идет инициализация даже с Delay в 2 секунды.
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingBackgroundService(
        IBookingProcessingService bookingProcessingService,
        IEventService eventService,
        ILogger<BookingBackgroundService> logger)
    {
        _bookingProcessingService = bookingProcessingService;
        _eventService = eventService;
        _logger = logger;
    }
    /// <summary>
    /// Каждые N секунд проверяет, есть ли новые бронирования
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = await _bookingProcessingService.GetPendingBookingsAsync();

            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));

            await Task.WhenAll(tasks);

            await Task.Delay(PollingInterval, stoppingToken); 
        }
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        
        try
        {
            if (stoppingToken.IsCancellationRequested)
                return;
            await Task.Delay(ProcessingDelay, stoppingToken); //ОООООЧЕНЬ тяжелая операция
            Random payment = new Random();
            if (payment.Next(0, 6) == 5)
                throw new PaymentRejectedException("Бронь отменена, не прошла оплата (Random)");

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var existingEvent = await _eventService.GetByIdAsync(booking.EventId); //Вместо InMemoryEventStore
                if (existingEvent is null)
                {
                    _logger.LogWarning($"Мероприятие с id {booking.EventId} не найдено, отменяю бронирование с id {booking.Id}");
                    var isRejected = await _bookingProcessingService.RejectBookingAsync(booking.Id);
                    if (isRejected)
                        _logger.LogInformation($"Бронь с id {booking.Id} отменена");
                    return;
                }

                var isConfirmed = await _bookingProcessingService.ConfirmBookingAsync(booking.Id);
                if (isConfirmed)
                    _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (PaymentRejectedException exception)
        {
            _logger.LogError(exception, $"Ошибка при оплате брони с id {booking.Id}, отменяю бронирование");
            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var existingEvent = await _eventService.GetByIdAsync(booking.EventId);
                if (existingEvent is not null)
                    existingEvent.ReleaseSeats();
                var isRejected = await _bookingProcessingService.RejectBookingAsync(booking.Id);
                if (isRejected)
                    _logger.LogInformation($"Бронь с id {booking.Id} отменена");
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {booking.Id}");
            //Тут я не хочу отменять, так как неизвестно, по какому именно условию мы можем попасть в общий Exception, а значит сам Accept\Reject метод может дать exception
        }
    }
}

public class PaymentRejectedException : Exception
{
    public PaymentRejectedException(string message) : base(message)
    {
    }
}
