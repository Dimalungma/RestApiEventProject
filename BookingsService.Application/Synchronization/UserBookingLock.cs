namespace BookingsService.Application;

public sealed class UserBookingLock : IUserBookingLock
{
    private readonly object _sync = new();
    private readonly Dictionary<long, LockEntry> _locks = new();

    public async Task<IDisposable> AcquireAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        LockEntry entry;

        lock (_sync)
        {
            if (!_locks.TryGetValue(userId, out entry!))
            {
                entry = new LockEntry();
                _locks.Add(userId, entry);
            }

            entry.ReferenceCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);

            return new Releaser(this, userId, entry);
        }
        catch
        {
            ReleaseReference(userId, entry);
            throw;
        }
    }

    private void Release(
        long userId,
        LockEntry entry)
    {
        entry.Semaphore.Release();

        ReleaseReference(userId, entry);
    }

    private void ReleaseReference(
        long userId,
        LockEntry entry)
    {
        lock (_sync)
        {
            entry.ReferenceCount--;

            if (entry.ReferenceCount != 0)
            {
                return;
            }

            _locks.Remove(userId);
            entry.Semaphore.Dispose();
        }
    }

    private sealed class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }

    private sealed class Releaser : IDisposable
    {
        private readonly UserBookingLock _owner;
        private readonly long _userId;
        private readonly LockEntry _entry;
        private bool _disposed;

        public Releaser(
            UserBookingLock owner,
            long userId,
            LockEntry entry)
        {
            _owner = owner;
            _userId = userId;
            _entry = entry;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _owner.Release(_userId, _entry);
        }
    }
}