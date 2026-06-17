using RestApiEventProject.DataAccess;
using RestApiEventProject.Models;
using Microsoft.EntityFrameworkCore;

namespace RestApiEventProject.Services;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService, IBookingProcessingService
{
    private readonly AppDbContext _context;

    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId)
    {
        
        await BookingSemaphore.WaitAsync();

        try
        {
            var existingEvent = await _context.Events.FindAsync(eventId);
            if (existingEvent is null)
            {
                return (null, BookingCreateError.EventNotFound);
            }

            if (!existingEvent.TryReserveSeats())
            {
                return (null, BookingCreateError.NoAvailableSeats);
            }

            var lastId = await _context.Bookings
                .OrderByDescending(b => b.Id)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            var booking = Booking.CreatePending(lastId + 1, eventId);

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            return (booking, null);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        return await _context.Bookings.FindAsync(bookingId);
    }

    //Вынес в отдельный интерфейс, чтобы потом можно было красиво разделить при переходе на EF, а сейчас не снимать private с _bookings

    [Obsolete]
    public async Task<IReadOnlyCollection<Booking>> GetPendingBookingsAsync()
    {
        return await _context.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .ToListAsync();
    }

    [Obsolete]
    public async Task<bool> ConfirmBookingAsync(long bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);

        if (booking is null)
        {
            return false;
        }

        booking.Confirm();

        await _context.SaveChangesAsync();

        return true;
    }

    [Obsolete]
    public async Task<bool> RejectBookingAsync(long bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);

        if (booking is null)
        {
            return false;
        }

        booking.Reject();

        await _context.SaveChangesAsync();

        return true;
    }
}