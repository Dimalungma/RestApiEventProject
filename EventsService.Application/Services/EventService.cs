using EventsService.Domain;

namespace EventsService.Application;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository; 
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public EventService(
        IEventRepository eventRepository,
        ICacheService cacheService,
        CacheOptions cacheOptions)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions;
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query)
    {
        return await _eventRepository.GetAllAsync(query);
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        var cacheKey = EventCacheKeys.ById(id);

        var cachedEvent = await _cacheService.GetAsync<Event>(cacheKey);

        if (cachedEvent is not null)
            return cachedEvent;

        var eventItem = await _eventRepository.GetByIdAsync(id);

        if (eventItem is null)
            return null;

        await _cacheService.SetAsync(
            cacheKey,
            eventItem,
            TimeSpan.FromMinutes(_cacheOptions.EventTtlMinutes));

        return eventItem;
    }

    public async Task<IReadOnlyCollection<Event>> GetTop10Async()
    {
        var cachedEvents =
            await _cacheService.GetAsync<List<Event>>(EventCacheKeys.Top10);

        if (cachedEvents is not null)
            return cachedEvents;

        var topEvents =
            (await _eventRepository.GetTop10Async()).ToList();

        await _cacheService.SetAsync(
            EventCacheKeys.Top10,
            topEvents,
            TimeSpan.FromMinutes(_cacheOptions.TopEventsTtlMinutes));

        return topEvents;
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        await _eventRepository.AddAsync(eventItem);

        await _eventRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(
            EventCacheKeys.ById(eventItem.Id));

        return eventItem;
    }

    public async Task<EventUpdateResult> UpdateAsync(int id, Event eventItem)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(id);

        if (existingEvent is null)
        {
            return EventUpdateResult.NotFound;
        }

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;
        var result = existingEvent.TryChangeTotalSeats(eventItem.TotalSeats);
        if (result == ChangeTotalSeatsResult.InvalidTotalSeats)
            return EventUpdateResult.InvalidTotalSeats;
        if (result == ChangeTotalSeatsResult.TotalSeatsLessThanReservedSeats)
            return EventUpdateResult.TotalSeatsLessThanReservedSeats;

        // Тут специально не трогаю AvailableSeats.
        await _eventRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(
            EventCacheKeys.ById(id));

        return EventUpdateResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingEvent = await _eventRepository.GetByIdAsync(id);

        if (existingEvent is null)
        {
            return false;
        }

        _eventRepository.Delete(existingEvent);

        await _eventRepository.SaveChangesAsync();

        await _cacheService.RemoveAsync(
            EventCacheKeys.ById(id));

        return true;
    }
}
