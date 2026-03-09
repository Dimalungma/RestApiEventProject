using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public class EventService : IEventService
{
    private List<Event> events = []; //Пока так, дальше будет репозиторий\база
    public IEnumerable<Event> GetAll()
    {
        // вернуть список
        throw new NotImplementedException();
    }

    public Event? GetById(int id)
    {
        // найти по id
        throw new NotImplementedException();
    }

    public Event Create(Event eventItem)
    {
        // назначить id
        // добавить в список
        // вернуть созданный объект
        throw new NotImplementedException();
    }

    public bool Update(int id, Event eventItem)
    {
        // найти существующее событие
        // если нет -> false
        // иначе обновить поля и вернуть true
        throw new NotImplementedException();
    }

    public bool Delete(int id)
    {
        // найти и удалить
        // true / false
        throw new NotImplementedException();
    }
}
