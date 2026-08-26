using EventsService.Application;
using EventsService.Domain;

namespace RestApiProject.Tests.Infrastructure;

internal sealed class TrackingCacheService : ICacheService
{
    private readonly Dictionary<string, object> _values = [];
    private readonly List<string>? _operationLog;

    public TrackingCacheService(List<string>? operationLog = null)
    {
        _operationLog = operationLog;
    }

    public int GetCalls { get; private set; }

    public int SetCalls { get; private set; }

    public int RemoveCalls { get; private set; }

    public string? LastGetKey { get; private set; }

    public string? LastSetKey { get; private set; }

    public string? LastRemovedKey { get; private set; }

    public object? LastSetValue { get; private set; }

    public TimeSpan? LastExpiration { get; private set; }

    public void SetGetResult<T>(string key, T value)
        where T : notnull
    {
        _values[key] = value;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        GetCalls++;
        LastGetKey = key;

        if (_values.TryGetValue(key, out var value) &&
            value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration)
    {
        SetCalls++;
        LastSetKey = key;
        LastSetValue = value;
        LastExpiration = expiration;

        _values[key] = value!;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        RemoveCalls++;
        LastRemovedKey = key;

        _operationLog?.Add($"Cache.Remove:{key}");

        _values.Remove(key);

        return Task.CompletedTask;
    }
}

internal sealed class TrackingEventRepository : IEventRepository
{
    private readonly List<string>? _operationLog;

    public TrackingEventRepository(List<string>? operationLog = null)
    {
        _operationLog = operationLog;
    }

    public Event? EventById { get; set; }

    public IReadOnlyCollection<Event> Top10Result { get; set; } =
        Array.Empty<Event>();

    public int NewEventId { get; set; } = 1;

    public int GetByIdCalls { get; private set; }

    public int GetTop10Calls { get; private set; }

    public int AddCalls { get; private set; }

    public int DeleteCalls { get; private set; }

    public int SaveChangesCalls { get; private set; }

    public Event? AddedEvent { get; private set; }

    public Event? DeletedEvent { get; private set; }

    public Task<PaginatedResult<Event>> GetAllAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "GetAllAsync не используется в тестах кеширования");
    }

    public Task<Event?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetByIdCalls++;

        return Task.FromResult(EventById);
    }

    public Task<IReadOnlyCollection<Event>> GetTop10Async(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetTop10Calls++;

        return Task.FromResult(Top10Result);
    }

    public Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AddCalls++;
        AddedEvent = eventItem;

        if (eventItem.Id == 0)
        {
            eventItem.Id = NewEventId;
        }

        return Task.CompletedTask;
    }

    public void Delete(Event eventItem)
    {
        DeleteCalls++;
        DeletedEvent = eventItem;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCalls++;

        _operationLog?.Add("Repository.SaveChanges");

        return Task.CompletedTask;
    }
}

internal sealed class TrackingBookingReservationRepository
    : IBookingReservationRepository
{
    private readonly List<string>? _operationLog;

    public TrackingBookingReservationRepository(
        List<string>? operationLog = null)
    {
        _operationLog = operationLog;
    }

    public BookingReservation? ReservationToReturn { get; set; }

    public BookingReservation? AddedReservation { get; private set; }

    public int GetByBookingIdCalls { get; private set; }

    public int AddCalls { get; private set; }

    public int SaveChangesCalls { get; private set; }

    public Task<BookingReservation?> GetByBookingIdAsync(
        long bookingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetByBookingIdCalls++;

        return Task.FromResult(ReservationToReturn);
    }

    public Task AddAsync(
        BookingReservation reservation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AddCalls++;
        AddedReservation = reservation;
        ReservationToReturn = reservation;

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCalls++;

        _operationLog?.Add("ReservationRepository.SaveChanges");

        return Task.CompletedTask;
    }
}

internal sealed class TestEventSeatEventPublisher
    : IEventSeatEventPublisher
{
    public int ReservedPublishCalls { get; private set; }

    public int UnavailablePublishCalls { get; private set; }

    public long? LastBookingId { get; private set; }

    public int? LastEventId { get; private set; }

    public string? LastReason { get; private set; }

    public Task PublishEventSeatReservedAsync(
        long bookingId,
        int eventId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ReservedPublishCalls++;
        LastBookingId = bookingId;
        LastEventId = eventId;

        return Task.CompletedTask;
    }

    public Task PublishEventSeatUnavailableAsync(
        long bookingId,
        int eventId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        UnavailablePublishCalls++;
        LastBookingId = bookingId;
        LastEventId = eventId;
        LastReason = reason;

        return Task.CompletedTask;
    }
}