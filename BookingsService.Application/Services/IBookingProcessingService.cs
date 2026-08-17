namespace BookingsService.Application;

public interface IBookingProcessingService
{
    Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default);

    Task ProcessBookingAsync(long bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<long>> GetAwaitingConfirmationWithoutRequestIdsAsync(
        CancellationToken cancellationToken = default);
}
