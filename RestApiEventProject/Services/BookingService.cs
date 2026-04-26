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

    public BookingService(IEventService eventService)
    {
        this.eventService = eventService;
    }

    public async Task<Booking?> CreateBookingAsync(int eventId)
    {
        var existingEvent = await eventService.GetByIdAsync(eventId);

        if (existingEvent is null)
        {
            return null;
        }

        var booking = new Booking
        {
            Id = Interlocked.Increment(ref currentId),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        _bookings.TryAdd(booking.Id, booking);

        return booking;
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

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }
}