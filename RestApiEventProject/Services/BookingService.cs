using RestApiEventProject.DTO;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IEventService eventService;
    private readonly List<Booking> bookings = [];
    private long currentId = 0;

    public BookingService(IEventService eventService)
    {
        this.eventService = eventService;
    }

    public async Task<BookingInfoDto?> CreateBookingAsync(int eventId)
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

        bookings.Add(booking);

        return MapToDto(booking);
    }

    public Task<BookingInfoDto?> GetBookingByIdAsync(long bookingId)
    {
        var booking = bookings.FirstOrDefault(booking => booking.Id == bookingId);

        return Task.FromResult(booking is null ? null : MapToDto(booking));
    }

    private static BookingInfoDto MapToDto(Booking booking)
    {
        return new BookingInfoDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}