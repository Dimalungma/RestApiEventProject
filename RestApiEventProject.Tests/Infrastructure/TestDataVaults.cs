using BookingsService.Application;
using BookingsService.Domain;
using EventsService.Application;
using EventsService.Domain;
using UsersService.Application;
using UsersService.Domain;

namespace RestApiProject.Tests.Infrastructure;

// ─────────────────────────── Events ───────────────────────────

internal sealed class TestEventStore
{
    public Dictionary<int, Event> Events { get; } = [];

    public int NextId { get; set; } = 1;
}

internal sealed class TestEventRepository : IEventRepository
{
    private readonly TestEventStore _store;

    //Имитируем tracked entities DbContext в пределах одного scope
    private readonly Dictionary<int, Event> _trackedEvents = [];
    private readonly HashSet<int> _deletedEventIds = [];

    public TestEventRepository(TestEventStore store)
    {
        _store = store;
    }

    public Task<PaginatedResult<Event>> GetAllAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<Event> events =
            _store.Events.Values.Select(CloneEvent);

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            events = events.Where(eventItem =>
                eventItem.Title.Contains(
                    query.Title,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            events = events.Where(eventItem =>
                eventItem.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(eventItem =>
                eventItem.EndAt <= query.To.Value);
        }

        var filteredEvents = events
            .OrderBy(eventItem => eventItem.Id)
            .ToList();

        var pagedEvents = filteredEvents
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<Event>
        {
            TotalCount = filteredEvents.Count,
            Page = query.Page,
            CurrentItemCount = pagedEvents.Count,
            Items = pagedEvents.AsReadOnly()
        });
    }

    public Task<Event?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_deletedEventIds.Contains(id))
        {
            return Task.FromResult<Event?>(null);
        }

        if (_trackedEvents.TryGetValue(id, out var trackedEvent))
        {
            return Task.FromResult<Event?>(trackedEvent);
        }

        if (!_store.Events.TryGetValue(id, out var storedEvent))
        {
            return Task.FromResult<Event?>(null);
        }

        trackedEvent = CloneEvent(storedEvent);
        _trackedEvents[id] = trackedEvent;

        return Task.FromResult<Event?>(trackedEvent);
    }

    public Task<IReadOnlyCollection<Event>> GetTop10Async(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<Event> result = _store.Events.Values
            .Where(eventItem => eventItem.TotalSeats > 0)
            .OrderByDescending(eventItem =>
                (double)(eventItem.TotalSeats - eventItem.AvailableSeats) /
                eventItem.TotalSeats)
            .ThenBy(eventItem => eventItem.Id)
            .Take(10)
            .Select(CloneEvent)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (eventItem.Id == 0)
        {
            eventItem.Id = _store.NextId++;
        }

        _trackedEvents[eventItem.Id] = eventItem;

        return Task.CompletedTask;
    }

    public void Delete(Event eventItem)
    {
        _trackedEvents.Remove(eventItem.Id);
        _deletedEventIds.Add(eventItem.Id);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var eventId in _deletedEventIds)
        {
            _store.Events.Remove(eventId);
        }

        foreach (var (eventId, eventItem) in _trackedEvents)
        {
            if (_deletedEventIds.Contains(eventId))
            {
                continue;
            }

            _store.Events[eventId] = CloneEvent(eventItem);
        }

        return Task.CompletedTask;
    }

    private static Event CloneEvent(Event source)
    {
        return new Event(
            source.Title,
            source.Description,
            source.StartAt,
            source.EndAt,
            source.TotalSeats)
        {
            Id = source.Id,
            AvailableSeats = source.AvailableSeats
        };
    }
}

internal sealed class TestCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }
}

// ─────────────────────────── Bookings ───────────────────────────

internal sealed class TestBookingStore
{
    public Dictionary<long, Booking> Bookings { get; } = [];

    public long NextId { get; set; } = 1;
}

internal sealed class TestBookingRepository : IBookingRepository
{
    private readonly TestBookingStore _store;
    private readonly List<Booking> _addedBookings = [];

    public TestBookingRepository(TestBookingStore store)
    {
        _store = store;
    }

    public Task<Booking?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _store.Bookings.TryGetValue(id, out var booking);

        return Task.FromResult(booking);
    }

    public Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (booking.Id == 0)
        {
            booking.Id = _store.NextId++;
        }

        _addedBookings.Add(booking);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<long>> GetPendingBookingIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<long> result = _store.Bookings.Values
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var booking in _addedBookings)
        {
            _store.Bookings[booking.Id] = booking;
        }

        _addedBookings.Clear();

        return Task.CompletedTask;
    }

    public Task<int> GetActiveBookingsCountByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = _store.Bookings.Values.Count(booking =>
            booking.UserId == userId &&
            (booking.Status == BookingStatus.Pending ||
             booking.Status == BookingStatus.AwaitingConfirmation ||
             booking.Status == BookingStatus.Confirmed));

        return Task.FromResult(count);
    }

    public Task<IReadOnlyCollection<long>>
        GetAwaitingConfirmationWithoutRequestIdsAsync(
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<long> result = _store.Bookings.Values
            .Where(booking =>
                booking.Status == BookingStatus.AwaitingConfirmation &&
                booking.ConfirmationRequestedAt is null)
            .Select(booking => booking.Id)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }
}

internal sealed class TestBookingEventPublisher : IBookingEventPublisher
{
    public Task PublishBookingCreatedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishBookingConfirmedAsync(
        long bookingId,
        int eventId,
        long userId,
        int seatsCount,
        DateTime confirmedAtUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishBookingRejectedAsync(
        long bookingId,
        int eventId,
        long userId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishBookingCancelledAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class TestUserBookingLock : IUserBookingLock
{
    public Task<IDisposable> AcquireAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IDisposable>(
            TestLockHandle.Instance);
    }

    private sealed class TestLockHandle : IDisposable
    {
        public static TestLockHandle Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

// ─────────────────────────── Users ───────────────────────────

internal sealed class TestUserStore
{
    public Dictionary<long, User> Users { get; } = [];

    public long NextId { get; set; } = 1;
}

internal sealed class TestUserRepository : IUserRepository
{
    private readonly TestUserStore _store;
    private readonly List<User> _addedUsers = [];

    public TestUserRepository(TestUserStore store)
    {
        _store = store;
    }

    public Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = _store.Users.Values.FirstOrDefault(existingUser =>
            existingUser.Login == login);

        return Task.FromResult(user);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user.Id == 0)
        {
            user.Id = _store.NextId++;
        }

        _addedUsers.Add(user);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var user in _addedUsers)
        {
            _store.Users[user.Id] = user;
        }

        _addedUsers.Clear();

        return Task.CompletedTask;
    }
}

internal sealed class TestPasswordHasher : IPasswordHasher
{
    private const string Prefix = "TEST_HASH:";

    public string Hash(string password)
    {
        return $"{Prefix}{password}";
    }

    public bool Verify(string password, string passwordHash)
    {
        return passwordHash == Hash(password);
    }
}