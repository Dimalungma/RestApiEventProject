using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Extensions;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Event> queryable = _context.Events;

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            queryable = queryable.Where(e => EF.Functions.ILike(e.Title, $"%{query.Title}%"));
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value.ToUtcNormalized(); //Забыл про то, что напрямую EF кастомные функции не применит

            queryable = queryable.Where(e => e.StartAt >= from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.ToUtcNormalized(); //Забыл про то, что напрямую EF кастомные функции не применит, le упс

            queryable = queryable.Where(e => e.EndAt <= to);

        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var pagedEvents = await queryable
            .OrderBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Page = query.Page,
            CurrentItemCount = pagedEvents.Count,
            Items = pagedEvents.AsReadOnly()
        };
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Events.FindAsync([id], cancellationToken);
    }

    public async Task<int> GetLastIdAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .OrderByDescending(e => e.Id)
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(eventItem, cancellationToken);
    }

    public void Delete(Event eventItem)
    {
        _context.Events.Remove(eventItem);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}