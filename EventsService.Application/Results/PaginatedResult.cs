namespace EventsService.Application;

public class PaginatedResult<T>
{
    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int CurrentItemCount { get; set; }

    public IEnumerable<T> Items { get; set; } = [];
}
