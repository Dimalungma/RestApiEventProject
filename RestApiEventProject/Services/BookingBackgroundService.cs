using Microsoft.EntityFrameworkCore;
using RestApiEventProject.DataAccess;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5); 
    //Увеличил до 5 в рамках отладки, так как нереально отловить момент смены, сразу идет инициализация даже с Delay в 2 секунды.
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

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

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingBookingIds = await context.Bookings
                .Where(booking => booking.Status == BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToListAsync(stoppingToken);

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

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings
                .FirstOrDefaultAsync(booking => booking.Id == bookingId, stoppingToken);

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

            var existingEvent = await context.Events
                .FirstOrDefaultAsync(existingEvent => existingEvent.Id == booking.EventId, stoppingToken);

            if (existingEvent is null)
            {
                _logger.LogWarning($"Мероприятие с id {booking.EventId} не найдено, отменяю бронирование с id {booking.Id}");

                booking.Reject();

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation($"Бронь с id {booking.Id} отменена");

                return;
            }

            booking.Confirm();

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (PaymentRejectedException exception)
        {
            _logger.LogError(exception, $"Ошибка при оплате брони с id {bookingId}, отменяю бронирование");

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings
                .FirstOrDefaultAsync(booking => booking.Id == bookingId, stoppingToken);

            if (booking is null)
                return;

            var existingEvent = await context.Events
                .FirstOrDefaultAsync(existingEvent => existingEvent.Id == booking.EventId, stoppingToken);

            if (existingEvent is not null)
                existingEvent.ReleaseSeats();

            booking.Reject();

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation($"Бронь с id {booking.Id} отменена");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {bookingId}");
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
