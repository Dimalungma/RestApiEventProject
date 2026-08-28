using EventsService.Domain;
using EventsService.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using RestApiEventProject.IntegrationTests.Infrastructure;

namespace RestApiEventProject.IntegrationTests;

public class RedisCacheServiceIntegrationTests
    : IClassFixture<RedisTestFixture>
{
    private readonly RedisTestFixture _fixture;

    public RedisCacheServiceIntegrationTests(
        RedisTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Save_And_Return_Event()
    {
        // Arrange
        var cacheService = CreateCacheService();

        var key =
            $"integration:event:{Guid.NewGuid():N}";

        var eventItem = new Event(
            "Интеграционный Redis",
            "Проверка сериализации события",
            FutureUtcDate(1, 10),
            FutureUtcDate(1, 12),
            10)
        {
            Id = 5,
            AvailableSeats = 7
        };

        // Act
        await cacheService.SetAsync(
            key,
            eventItem,
            TimeSpan.FromMinutes(1));

        var result =
            await cacheService.GetAsync<Event>(key);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            eventItem.Id,
            result.Id);

        Assert.Equal(
            eventItem.Title,
            result.Title);

        Assert.Equal(
            eventItem.Description,
            result.Description);

        Assert.Equal(
            eventItem.StartAt,
            result.StartAt);

        Assert.Equal(
            eventItem.EndAt,
            result.EndAt);

        Assert.Equal(
            eventItem.TotalSeats,
            result.TotalSeats);

        Assert.Equal(
            eventItem.AvailableSeats,
            result.AvailableSeats);
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Save_And_Return_Event_List()
    {
        // Arrange
        var cacheService = CreateCacheService();

        var key =
            $"integration:events:top10:{Guid.NewGuid():N}";

        var events = new List<Event>
        {
            new(
                "Первое событие",
                null,
                FutureUtcDate(1, 10),
                FutureUtcDate(1, 12),
                100)
            {
                Id = 1,
                AvailableSeats = 10
            },
            new(
                "Второе событие",
                null,
                FutureUtcDate(2, 10),
                FutureUtcDate(2, 12),
                50)
            {
                Id = 2,
                AvailableSeats = 20
            }
        };

        // Act
        await cacheService.SetAsync(
            key,
            events,
            TimeSpan.FromMinutes(1));

        var result =
            await cacheService.GetAsync<List<Event>>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(
            events[0].Id,
            result[0].Id);

        Assert.Equal(
            events[0].Title,
            result[0].Title);

        Assert.Equal(
            events[0].AvailableSeats,
            result[0].AvailableSeats);

        Assert.Equal(
            events[1].Id,
            result[1].Id);

        Assert.Equal(
            events[1].Title,
            result[1].Title);

        Assert.Equal(
            events[1].AvailableSeats,
            result[1].AvailableSeats);
    }

    [Fact]
    public async Task RemoveAsync_Should_Delete_Value_From_Redis()
    {
        // Arrange
        var cacheService = CreateCacheService();

        var key =
            $"integration:event:{Guid.NewGuid():N}";

        var eventItem = new Event(
            "На удаление из кеша",
            null,
            FutureUtcDate(1, 10),
            FutureUtcDate(1, 12),
            10)
        {
            Id = 10
        };

        await cacheService.SetAsync(
            key,
            eventItem,
            TimeSpan.FromMinutes(1));

        var valueBeforeRemove =
            await cacheService.GetAsync<Event>(key);

        Assert.NotNull(valueBeforeRemove);

        // Act
        await cacheService.RemoveAsync(key);

        var valueAfterRemove =
            await cacheService.GetAsync<Event>(key);

        // Assert
        Assert.Null(valueAfterRemove);
    }

    [Fact]
    public async Task SetAsync_Should_Expire_Value_After_Ttl()
    {
        // Arrange
        var cacheService = CreateCacheService();

        var key =
            $"integration:event:ttl:{Guid.NewGuid():N}";

        var eventItem = new Event(
            "Событие с TTL",
            null,
            FutureUtcDate(1, 10),
            FutureUtcDate(1, 12),
            10)
        {
            Id = 15
        };

        await cacheService.SetAsync(
            key,
            eventItem,
            TimeSpan.FromSeconds(1));

        var valueBeforeExpiration =
            await cacheService.GetAsync<Event>(key);

        Assert.NotNull(valueBeforeExpiration);

        // Act
        Event? valueAfterExpiration = null;

        var timeoutAt =
            DateTime.UtcNow.AddSeconds(5);

        do
        {
            await Task.Delay(100);

            valueAfterExpiration =
                await cacheService.GetAsync<Event>(key);

            if (valueAfterExpiration is null)
            {
                break;
            }
        }
        while (DateTime.UtcNow < timeoutAt);

        // Assert
        Assert.Null(valueAfterExpiration);
    }

    private RedisCacheService CreateCacheService()
    {
        return new RedisCacheService(
            _fixture.ConnectionMultiplexer,
            NullLogger<RedisCacheService>.Instance);
    }

    private static DateTime FutureUtcDate(
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