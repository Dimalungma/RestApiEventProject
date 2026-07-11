using RestApiEventProject.Domain;

namespace RestApiEventProject.Application;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> GetActiveBookingsCountByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);
}