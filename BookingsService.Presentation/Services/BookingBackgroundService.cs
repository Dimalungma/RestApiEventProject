using BookingsService.Application;

namespace BookingsService.Presentation.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;

    //Увеличил до 10 в рамках отладки, так как нереально отловить момент смены, сразу идет инициализация даже с Delay в 2 секунды, а я не успеваю промотать до GET в swagger'е))
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

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
            try
            {
                var pendingBookingIds = await GetPendingBookingIdsAsync(stoppingToken);

                var tasks = pendingBookingIds.Select(bookingId => ProcessBookingInNewScopeAsync(bookingId, stoppingToken));

                await Task.WhenAll(tasks);

                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Неизвестная ошибка при работе фонового сервиса обработки бронирований");

                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

        return await bookingProcessingService.GetPendingBookingIdsAsync(stoppingToken);
    }

    private async Task ProcessBookingInNewScopeAsync(long bookingId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

        await bookingProcessingService.ProcessBookingAsync(bookingId, stoppingToken);
    }
}