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

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }


    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId)
    {
        
        await BookingSemaphore.WaitAsync();

        try
        {
            var existingEvent = await _eventRepository.GetByIdAsync(eventId);

            if (existingEvent is null)
            {
                return (null, BookingCreateError.EventNotFound);
            }

            if (!existingEvent.TryReserveSeats())
            {
                return (null, BookingCreateError.NoAvailableSeats);
            }

            var lastId = await _bookingRepository.GetLastIdAsync();

            var booking = Booking.CreatePending(lastId + 1, eventId);

            await _bookingRepository.AddAsync(booking);

            await _bookingRepository.SaveChangesAsync();

            return (booking, null);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }
}