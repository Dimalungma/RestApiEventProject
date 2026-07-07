using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Application;

namespace RestApiEventProject.Presentation.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;

    //Увеличил до 5 в рамках отладки, так как нереально отловить момент смены, сразу идет инициализация даже с Delay в 2 секунды.
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5); 
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
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
            using var scope = _scopeFactory.CreateScope();

            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var pendingBookingIds = await bookingRepository.GetPendingBookingIdsAsync(stoppingToken);

            var tasks = pendingBookingIds.Select(bookingId => ProcessBookingAsync(bookingId, stoppingToken));

            await Task.WhenAll(tasks);

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(long bookingId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);

            if (booking is null)
            {
                _logger.LogWarning($"Бронь с id {bookingId} не найдена");

                return;
            }
            _logger.LogInformation($"Начата фоновая обработка брони с id {booking.Id} для мероприятия с id {booking.EventId}");
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Бронь с id {booking.Id} для мероприятия с id {booking.EventId} убрана из фоновой обработки, прислали CancellationToken");
                return;
            }
            await Task.Delay(ProcessingDelay, stoppingToken); //ОООООЧЕНЬ тяжелая операция
            Random payment = new Random();
            if (payment.Next(0, 6) == 5)
                throw new PaymentRejectedException($"Бронь {booking.Id} отменена, не прошла оплата (Random)");

            var existingEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

            if (existingEvent is null)
            {
                _logger.LogWarning($"Мероприятие с id {booking.EventId} не найдено, отменяю бронирование с id {booking.Id}");

                booking.Reject();

                await bookingRepository.SaveChangesAsync(stoppingToken);

                _logger.LogInformation($"Бронь с id {booking.Id} отменена");

                return;
            }

            booking.Confirm();

            await bookingRepository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (PaymentRejectedException exception)
        {
            _logger.LogError(exception, $"Ошибка при оплате брони с id {bookingId}, отменяю бронирование");

            await RejectBookingAndReleaseSeatAsync(bookingId, stoppingToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {bookingId}, отменяю бронирование");

            await RejectBookingAndReleaseSeatAsync(bookingId, stoppingToken);
        }
    }

    private async Task RejectBookingAndReleaseSeatAsync(long bookingId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);

            if (booking is null)
                return;

            var existingEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

            if (existingEvent is not null)
                existingEvent.ReleaseSeats();

            booking.Reject();

            await bookingRepository.SaveChangesAsync(stoppingToken);

            _logger.LogInformation($"Бронь с id {booking.Id} отменена");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Не удалось отменить бронь с id {bookingId} после ошибки фоновой обработки");
        }
    }
}

public class PaymentRejectedException : Exception
{
    public PaymentRejectedException(string message) : base(message)
    {
    }
}
