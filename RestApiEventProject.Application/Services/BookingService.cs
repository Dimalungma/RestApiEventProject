using RestApiEventProject.Domain;
namespace RestApiEventProject.Application;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    private const int MaxActiveBookingsPerUser = 10;

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }


    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId, long userId)
    {
        
        await BookingSemaphore.WaitAsync();

        try
        {
            var existingEvent = await _eventRepository.GetByIdAsync(eventId);

            if (existingEvent is null)
            {
                return (null, BookingCreateError.EventNotFound);
            }

            if (existingEvent.StartAt <= DateTime.UtcNow)
            {
                return (null, BookingCreateError.EventAlreadyStarted);
            }

            var activeBookingsCount =
                await _bookingRepository.GetActiveBookingsCountByUserIdAsync(userId); //TODO метод проверки броней

            if (activeBookingsCount >= MaxActiveBookingsPerUser)
            {
                return (null, BookingCreateError.ActiveBookingsLimitExceeded);
            }

            if (!existingEvent.TryReserveSeats())
            {
                return (null, BookingCreateError.NoAvailableSeats);
            }

            var booking = Booking.CreatePending(eventId, userId);

            await _bookingRepository.AddAsync(booking);

            await _bookingRepository.SaveChangesAsync();

            return (booking, null);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingCancelError?> CancelBookingAsync(
        long bookingId,
        long userId,
        bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking is null)
        {
            return BookingCancelError.BookingNotFound;
        }

        if (!isAdmin && booking.UserId != userId) //Отменить может или сам пользователь, или администратор
        {
            return BookingCancelError.Forbidden;
        }

        var wasCancelled = booking.Cancel();

        if (!wasCancelled) //Если уже отменена раньше, и текущая проверка ничего не поменяла, освобождать места нельзя
        {
            return null;
        }

        var existingEvent = await _eventRepository.GetByIdAsync(booking.EventId);

        if (existingEvent is not null)
        {
            existingEvent.ReleaseSeats(1); //TODO а у нас может один юзер забронировать сразу много мест?
        }

        await _bookingRepository.SaveChangesAsync();

        return null;
    }

    public async Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }
}