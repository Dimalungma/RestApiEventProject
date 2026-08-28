namespace EventsService.Application;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int EventTtlMinutes { get; set; }
    public int TopEventsTtlMinutes { get; set; }
}