using EventsService.Domain;

namespace EventsService.Application;

public interface IBookingReservationRepository
{
    Task<BookingReservation?> GetByBookingIdAsync(long bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(BookingReservation reservation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}