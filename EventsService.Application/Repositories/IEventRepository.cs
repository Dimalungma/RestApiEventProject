using EventsService.Domain;

namespace EventsService.Application;

public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query, CancellationToken cancellationToken = default);

    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Event>> GetTop10Async(CancellationToken cancellationToken = default);

    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);

    void Delete(Event eventItem);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}