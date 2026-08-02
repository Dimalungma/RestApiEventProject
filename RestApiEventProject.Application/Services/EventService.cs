using RestApiEventProject.Domain;

namespace RestApiEventProject.Application;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query)
    {
        return await _eventRepository.GetAllAsync(query);
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _eventRepository.GetByIdAsync(id);
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        await _eventRepository.AddAsync(eventItem);

        await _eventRepository.SaveChangesAsync();

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

        return true;
    }
}
