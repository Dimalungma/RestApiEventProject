using Microsoft.EntityFrameworkCore;
using RestApiEventProject.DataAccess;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _context;
    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<Event>> GetAllAsync(GetEventsQuery query)
    {
        IQueryable<Event> queryable = _context.Events;

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

        var totalCount = await queryable.CountAsync();

        var pagedEvents = await queryable
            .OrderBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Page = query.Page,
            CurrentItemCount = pagedEvents.Count,
            Items = pagedEvents.AsReadOnly()
        };
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events.FindAsync(id);
    }

    public async Task<Event> CreateAsync(Event eventItem)
    {
        var lastId = await _context.Events
            .OrderByDescending(e => e.Id)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        eventItem.Id = lastId + 1; //Пока оставлю управление номером id в коде

        _context.Events.Add(eventItem);

        await _context.SaveChangesAsync();

        return eventItem;
    }

    public async Task<bool> UpdateAsync(int id, Event eventItem)
    {
        var existingEvent = await _context.Events.FindAsync(id);

        if (existingEvent is null)
        {
            return false;
        }

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;
        existingEvent.TotalSeats = eventItem.TotalSeats;
        existingEvent.AvailableSeats = eventItem.AvailableSeats;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existingEvent = await _context.Events.FindAsync(id);

        if (existingEvent is null)
        {
            return false;
        }

        _context.Events.Remove(existingEvent);

        await _context.SaveChangesAsync();

        return true;
    }
}
