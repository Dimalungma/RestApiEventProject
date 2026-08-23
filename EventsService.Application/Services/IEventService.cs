using EventsService.Domain;

namespace EventsService.Application;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query);

    Task<Event?> GetByIdAsync(int id);

    Task<IReadOnlyCollection<Event>> GetTop10Async();

    Task<Event> CreateAsync(Event eventItem);

    Task<EventUpdateResult> UpdateAsync(int id, Event eventItem);

    Task<bool> DeleteAsync(int id);
}
