using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.Services;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query);

    Task<Event?> GetByIdAsync(int id);

    Task<Event> CreateAsync(Event eventItem);

    Task<bool> UpdateAsync(int id, Event eventItem);

    Task<bool> DeleteAsync(int id);
}
