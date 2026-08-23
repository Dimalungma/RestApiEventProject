namespace EventsService.Application;

public static class EventCacheKeys
{
    public const string Top10 = "events:top10";

    public static string ById(int id) => $"event:{id}";
}