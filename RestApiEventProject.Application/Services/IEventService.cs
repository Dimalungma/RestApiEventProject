using RestApiEventProject.Domain;

namespace RestApiEventProject.Application;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query);

    Task<Event?> GetByIdAsync(int id);

    Task<Event> CreateAsync(Event eventItem);

    Task<EventUpdateResult> UpdateAsync(int id, Event eventItem);

    Task<bool> DeleteAsync(int id);
}
