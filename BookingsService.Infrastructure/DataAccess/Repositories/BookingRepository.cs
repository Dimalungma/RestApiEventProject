using Microsoft.EntityFrameworkCore;
using BookingsService.Application;
using BookingsService.Domain;

namespace BookingsService.Infrastructure.DataAccess;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(booking, cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveBookingsCountByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings.CountAsync(
            booking =>
                booking.UserId == userId &&
                (booking.Status == BookingStatus.Pending ||
                 booking.Status == BookingStatus.Confirmed),
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}