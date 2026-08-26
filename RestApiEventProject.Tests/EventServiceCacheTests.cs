using EventsService.Application;
using EventsService.Domain;
using FluentAssertions;
using RestApiProject.Tests.Infrastructure;

namespace RestApiProject.Tests;

public class EventServiceCacheTests
{
    private const int EventTtlMinutes = 5;
    private const int TopEventsTtlMinutes = 1;

    [Fact]
    public async Task GetByIdAsync_Should_Return_Cached_Event_Without_Calling_Repository()
    {
        // Arrange
        const int eventId = 5;

        var cachedEvent = CreateEvent(
            id: eventId,
            title: "Событие из кеша");

        var repository = new TrackingEventRepository();
        var cache = new TrackingCacheService();

        cache.SetGetResult(
            EventCacheKeys.ById(eventId),
            cachedEvent);

        var service = CreateService(repository, cache);

        // Act
        var result = await service.GetByIdAsync(eventId);

        // Assert
        result.Should().BeSameAs(cachedEvent);

        cache.GetCalls.Should().Be(1);
        cache.LastGetKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        repository.GetByIdCalls.Should().Be(0);

        cache.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Get_Event_From_Repository_And_Save_To_Cache_On_Cache_Miss()
    {
        // Arrange
        const int eventId = 5;

        var storedEvent = CreateEvent(
            id: eventId,
            title: "Событие из базы");

        var repository = new TrackingEventRepository
        {
            EventById = storedEvent
        };

        var cache = new TrackingCacheService();

        var service = CreateService(repository, cache);

        // Act
        var result = await service.GetByIdAsync(eventId);

        // Assert
        result.Should().BeSameAs(storedEvent);

        cache.GetCalls.Should().Be(1);
        repository.GetByIdCalls.Should().Be(1);

        cache.SetCalls.Should().Be(1);
        cache.LastSetKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        cache.LastSetValue.Should().BeSameAs(storedEvent);

        cache.LastExpiration.Should()
            .Be(TimeSpan.FromMinutes(EventTtlMinutes));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Not_Save_To_Cache_When_Event_Does_Not_Exist()
    {
        // Arrange
        const int eventId = 999;

        var repository = new TrackingEventRepository
        {
            EventById = null
        };

        var cache = new TrackingCacheService();

        var service = CreateService(repository, cache);

        // Act
        var result = await service.GetByIdAsync(eventId);

        // Assert
        result.Should().BeNull();

        cache.GetCalls.Should().Be(1);
        repository.GetByIdCalls.Should().Be(1);

        cache.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetTop10Async_Should_Return_Cached_Events_Without_Calling_Repository()
    {
        // Arrange
        var cachedEvents = new List<Event>
        {
            CreateEvent(1, "Первое"),
            CreateEvent(2, "Второе")
        };

        var repository = new TrackingEventRepository();
        var cache = new TrackingCacheService();

        cache.SetGetResult(
            EventCacheKeys.Top10,
            cachedEvents);

        var service = CreateService(repository, cache);

        // Act
        var result = await service.GetTop10Async();

        // Assert
        result.Should().BeSameAs(cachedEvents);

        cache.GetCalls.Should().Be(1);
        cache.LastGetKey.Should().Be(EventCacheKeys.Top10);

        repository.GetTop10Calls.Should().Be(0);

        cache.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetTop10Async_Should_Get_Events_From_Repository_And_Save_To_Cache_On_Cache_Miss()
    {
        // Arrange
        IReadOnlyCollection<Event> storedEvents =
            new List<Event>
            {
                CreateEvent(1, "Первое"),
                CreateEvent(2, "Второе")
            };

        var repository = new TrackingEventRepository
        {
            Top10Result = storedEvents
        };

        var cache = new TrackingCacheService();

        var service = CreateService(repository, cache);

        // Act
        var result = await service.GetTop10Async();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(storedEvents);

        cache.GetCalls.Should().Be(1);
        repository.GetTop10Calls.Should().Be(1);

        cache.SetCalls.Should().Be(1);
        cache.LastSetKey.Should().Be(EventCacheKeys.Top10);

        cache.LastSetValue.Should()
            .BeAssignableTo<List<Event>>();

        cache.LastExpiration.Should()
            .Be(TimeSpan.FromMinutes(TopEventsTtlMinutes));
    }

    [Fact]
    public async Task CreateAsync_Should_Invalidate_Event_Cache_After_Save()
    {
        // Arrange
        const int assignedEventId = 5;

        var operationLog = new List<string>();

        var repository =
            new TrackingEventRepository(operationLog)
            {
                NewEventId = assignedEventId
            };

        var cache =
            new TrackingCacheService(operationLog);

        var service = CreateService(repository, cache);

        var eventItem = CreateEvent(
            id: 0,
            title: "Новое событие");

        // Act
        var result = await service.CreateAsync(eventItem);

        // Assert
        result.Id.Should().Be(assignedEventId);

        repository.AddCalls.Should().Be(1);
        repository.SaveChangesCalls.Should().Be(1);

        cache.RemoveCalls.Should().Be(1);
        cache.LastRemovedKey.Should()
            .Be(EventCacheKeys.ById(assignedEventId));

        operationLog.Should().ContainInOrder(
            "Repository.SaveChanges",
            $"Cache.Remove:{EventCacheKeys.ById(assignedEventId)}");
    }

    [Fact]
    public async Task UpdateAsync_Should_Invalidate_Event_Cache_After_Save()
    {
        // Arrange
        const int eventId = 5;

        var operationLog = new List<string>();

        var existingEvent = CreateEvent(
            id: eventId,
            title: "Старое название");

        var repository =
            new TrackingEventRepository(operationLog)
            {
                EventById = existingEvent
            };

        var cache =
            new TrackingCacheService(operationLog);

        var service = CreateService(repository, cache);

        var updatedEvent = new Event(
            "Новое название",
            "Новое описание",
            FutureUtcDate(2, 10),
            FutureUtcDate(2, 12),
            20);

        // Act
        var result =
            await service.UpdateAsync(eventId, updatedEvent);

        // Assert
        result.Should().Be(EventUpdateResult.Success);

        repository.GetByIdCalls.Should().Be(1);
        repository.SaveChangesCalls.Should().Be(1);

        cache.RemoveCalls.Should().Be(1);
        cache.LastRemovedKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        operationLog.Should().ContainInOrder(
            "Repository.SaveChanges",
            $"Cache.Remove:{EventCacheKeys.ById(eventId)}");
    }

    [Fact]
    public async Task DeleteAsync_Should_Invalidate_Event_Cache_After_Save()
    {
        // Arrange
        const int eventId = 5;

        var operationLog = new List<string>();

        var existingEvent = CreateEvent(
            id: eventId,
            title: "На удаление");

        var repository =
            new TrackingEventRepository(operationLog)
            {
                EventById = existingEvent
            };

        var cache =
            new TrackingCacheService(operationLog);

        var service = CreateService(repository, cache);

        // Act
        var result = await service.DeleteAsync(eventId);

        // Assert
        result.Should().BeTrue();

        repository.DeleteCalls.Should().Be(1);
        repository.DeletedEvent.Should()
            .BeSameAs(existingEvent);

        repository.SaveChangesCalls.Should().Be(1);

        cache.RemoveCalls.Should().Be(1);
        cache.LastRemovedKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        operationLog.Should().ContainInOrder(
            "Repository.SaveChanges",
            $"Cache.Remove:{EventCacheKeys.ById(eventId)}");
    }

    private static EventService CreateService(
        IEventRepository repository,
        ICacheService cache)
    {
        var cacheOptions = new CacheOptions
        {
            EventTtlMinutes = EventTtlMinutes,
            TopEventsTtlMinutes = TopEventsTtlMinutes
        };

        return new EventService(
            repository,
            cache,
            cacheOptions);
    }

    private static Event CreateEvent(
        int id,
        string title)
    {
        return new Event(
            title,
            null,
            FutureUtcDate(1, 10),
            FutureUtcDate(1, 12),
            10)
        {
            Id = id
        };
    }

    private static DateTime FutureUtcDate( //Буду все таки динамически обновлять в зависимости от даты запуска теста, раз у нас теперь достаточно проверок на "дата не меньше чем текущая"
        int daysFromToday,
        int hour = 0,
        int minute = 0)
    {
        var date =
            DateTime.UtcNow.Date.AddDays(daysFromToday);

        return new DateTime(
            date.Year,
            date.Month,
            date.Day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}