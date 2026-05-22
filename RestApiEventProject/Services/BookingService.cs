using RestApiEventProject.Models;
using System.Collections.Concurrent;

namespace RestApiEventProject.Services;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService, IBookingProcessingService
{
    private readonly IEventService eventService; 
    private readonly ConcurrentDictionary<long, Booking> _bookings = new();
    private long currentId = 0;
    private readonly object _bookingLock = new();

    public BookingService(IEventService eventService)
    {
        this.eventService = eventService;
    }

    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId)
    {
        var existingEvent = await eventService.GetByIdAsync(eventId);

        if (existingEvent is null)
        {
            return (null, BookingCreateError.EventNotFound);
        }

        lock (_bookingLock) //Вообще конечно lock в async методе такое себе. 
        {                   //Если когда нибудь внутри появится await (а он кстати появится с добавлением EF) все сломается, как и говорилось в уроке
            if (!existingEvent.TryReserveSeats())
            {
                return (null, BookingCreateError.NoAvailableSeats);
            }
            var booking = Booking.CreatePending(
                Interlocked.Increment(ref currentId),
                eventId);

            _bookings.TryAdd(booking.Id, booking);
            return (booking, null);
        }
    }

    public Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            return Task.FromResult<Booking?>(null);
        return Task.FromResult<Booking?>(booking);
    }

    //Вынес в отдельный интерфейс, чтобы потом можно было красиво разделить при переходе на EF, а сейчас не снимать private с _bookings
    public Task<IReadOnlyCollection<Booking>> GetPendingBookingsAsync() 
    {
        var pendingBookings = _bookings.Values
            .Where(booking => booking.Status == BookingStatus.Pending)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<Booking>>(pendingBookings);
    }

    public Task<bool> ConfirmBookingAsync(long bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            return Task.FromResult(false);

        booking.Confirm();

        return Task.FromResult(true);
    }

    public Task<bool> RejectBookingAsync(long bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            return Task.FromResult(false);

        booking.Reject();

        return Task.FromResult(true);
    }
}