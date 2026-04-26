using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.Services;

public interface IEventService
{
    PaginatedResult<Event> GetAll(GetEventsQuery query);
    [Obsolete]
    Event? GetById(int id);
    public Task<Event?> GetByIdAsync(int id);
    Event Create(Event eventItem);
    bool Update(int id, Event eventItem);
    bool Delete(int id);
}
