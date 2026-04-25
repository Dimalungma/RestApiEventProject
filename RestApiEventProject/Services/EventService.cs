using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.Services;

public class EventService : IEventService
{
    private Dictionary<int, Event> _events = []; //Пока так, дальше будет репозиторий\база
    private int _nextId = 1; //Иначе рано или поздно удялят\создадут ивент, и при повторном обращении потеряю идемподентость
    public PaginatedResult<Event> GetAll(GetEventsQuery query)
    {
        IQueryable<Event> queryable = _events.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            queryable = queryable.Where(e =>
                e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            queryable = queryable.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            queryable = queryable.Where(e => e.EndAt <= query.To.Value);
        }

        var totalCount = queryable.Count();

        var pagedEvents = queryable
            .OrderBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList().AsReadOnly();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Page = query.Page,
            CurrentItemCount = pagedEvents.Count,
            Items = pagedEvents
        };
    }

    public Event? GetById(int id)
    {
        if (_events.ContainsKey(id))
            return _events[id];
        else
            return null;
    }

    public Event Create(Event eventItem)
    {
        eventItem.Id = _nextId;
        _events.Add(_nextId ,eventItem);
        _nextId++;
        return eventItem;
    }

    public bool Update(int id, Event eventItem)
    {
        if (_events.ContainsKey(id))
        {
            _events[id].Title = eventItem.Title;
            _events[id].Description = eventItem.Description;
            _events[id].StartAt = eventItem.StartAt;
            _events[id].EndAt = eventItem.EndAt;
            return true;
        }
        else
            return false;
    }

    public bool Delete(int id)
    {
        if (_events.ContainsKey(id))
        {
            _events.Remove(id);
            return true;
        }
        else
            return false;
    }
}
