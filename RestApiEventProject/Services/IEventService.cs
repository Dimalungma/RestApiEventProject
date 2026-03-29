using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public interface IEventService
{
    IEnumerable<Event> GetAll(GetEventsQuery query);
    Event? GetById(int id);
    Event Create(Event eventItem);
    bool Update(int id, Event eventItem);
    bool Delete(int id);
}
