using EventsService.Application;
using EventsService.Infrastructure.DataAccess;
using EventsService.Infrastructure.Messaging;
using EventsService.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingReservationRepository, BookingReservationRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = configuration.GetConnectionString("Redis")
                                   ?? throw new InvalidOperationException("Не настроена строка подключения Redis");

            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        var cacheOptions = configuration
                               .GetSection(CacheOptions.SectionName)
                               .Get<CacheOptions>()
                           ?? throw new InvalidOperationException("Не настроены параметры кеша");

        if (cacheOptions.EventTtlMinutes <= 0 ||
            cacheOptions.TopEventsTtlMinutes <= 0)
        {
            throw new InvalidOperationException("TTL кеша должен быть больше нуля");
        }

        services.AddSingleton(cacheOptions);

        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.SectionName));

        services.AddSingleton<IEventSeatEventPublisher, KafkaEventSeatEventPublisher>();

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<KafkaBookingLifecycleConsumer>();

        return services;
    }
}