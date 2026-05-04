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
                    Random payment = new Random();
                    if (payment.Next(0, 5) == 5)
                        throw new PaymentRejectedException("Бронь отменена, не прошла оплата (Random)");

                    var isConfirmed = await _bookingProcessingService.ConfirmBookingAsync(booking.Id);

                    if (isConfirmed)
                        _logger.LogInformation($"Бронь с id {booking.Id} подтверждена");
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (PaymentRejectedException exception)
                {
                    _logger.LogError(exception, $"Ошибка при оплате брони с id {booking.Id}, отменяю бронирование");
                    var isRejected = await _bookingProcessingService.RejectBookingAsync(booking.Id);
                    if (isRejected)
                        _logger.LogInformation($"Бронь с id {booking.Id} отменена");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Неизвестная ошибка при обработке брони с id {booking.Id}"); 
                    //Тут я не хочу отменять, так как неизвестно, по какому именно условию мы можем попасть в общий Exception, а значит сам Accept\Reject метод может дать exception
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); //Увеличил до 5 в рамках отладки, так как нереально отловить момент смены, сразу идет инициализация даже с Delay в 2 секунды.
        }
    }
}

public class PaymentRejectedException : Exception
{
    public PaymentRejectedException(string message) : base(message)
    {
    }
}
