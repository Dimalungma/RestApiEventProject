namespace RestApiEventProject.Services;

public class BookingBackgroundService : BackgroundService
{
    private readonly IBookingProcessingService _bookingProcessingService;
    private readonly ILogger<BookingBackgroundService> _logger;

    public BookingBackgroundService(
        IBookingProcessingService bookingProcessingService,
        ILogger<BookingBackgroundService> logger)
    {
        _bookingProcessingService = bookingProcessingService;
        _logger = logger;
    }
    /// <summary>
    /// Каждую секунду проверяет, есть ли новые бронирования
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = await _bookingProcessingService.GetPendingBookingsAsync();

            foreach (var booking in pendingBookings)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); //ОООООЧЕНЬ тяжелая операция

                    var isConfirmed = await _bookingProcessingService.ConfirmBookingAsync(booking.Id);

                    if (isConfirmed)
                        _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Ошибка при обработке брони с id {booking.Id}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
