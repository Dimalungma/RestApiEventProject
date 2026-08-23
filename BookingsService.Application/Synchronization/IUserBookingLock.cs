namespace BookingsService.Application;

public interface IUserBookingLock
{
    Task<IDisposable> AcquireAsync(
        long userId,
        CancellationToken cancellationToken = default);
}