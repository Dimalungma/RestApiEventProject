using System.Text.Json;
using EventsService.Application;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventsService.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (RedisException exception)
        {
            _logger.LogError(
                exception,
                $"Ошибка получения значения из Redis по ключу {key}");

            return default;
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                $"Ошибка десериализации значения Redis по ключу {key}");

            return default;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogError(
                exception,
                $"Неподдерживаемый тип значения Redis по ключу {key}");

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);

            await _database.StringSetAsync(
                key,
                serializedValue,
                expiration);
        }
        catch (RedisException exception)
        {
            _logger.LogError(
                exception,
                $"Ошибка записи значения в Redis по ключу {key}");
        }
        catch (NotSupportedException exception)
        {
            _logger.LogError(
                exception,
                $"Ошибка сериализации значения Redis по ключу {key}");
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisException exception)
        {
            _logger.LogError(
                exception,
                $"Ошибка удаления значения из Redis по ключу {key}");
        }
    }
}