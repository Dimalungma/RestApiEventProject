using StackExchange.Redis;
using Testcontainers.Redis;

namespace RestApiEventProject.IntegrationTests.Infrastructure;

public sealed class RedisTestFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer =
        new RedisBuilder("redis:7.2-alpine")
            .Build();

    public IConnectionMultiplexer ConnectionMultiplexer { get; private set; } =
        null!;

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        ConnectionMultiplexer =
            await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(
                _redisContainer.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        ConnectionMultiplexer.Dispose();

        await _redisContainer.DisposeAsync();
    }
}