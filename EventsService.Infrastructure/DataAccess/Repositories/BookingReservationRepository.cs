using EventsService.Application;
using EventsService.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Infrastructure.DataAccess;

public class BookingReservationRepository : IBookingReservationRepository
{
    private readonly AppDbContext _context;

    public BookingReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BookingReservation?> GetByBookingIdAsync(long bookingId, CancellationToken cancellationToken = default)
    {
        return await _context.BookingReservations.FirstOrDefaultAsync(reservation => reservation.BookingId == bookingId, cancellationToken);
    }

    public async Task AddAsync(BookingReservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.BookingReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}