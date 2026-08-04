namespace BookingsService.Application;

public interface IBookingProcessingService
{
    Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default);

    Task ProcessBookingAsync(long bookingId, CancellationToken cancellationToken = default);
}
