using EventsService.Application;
using EventsService.Infrastructure.DataAccess;
using EventsService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.SectionName));

        services.AddSingleton<IEventSeatEventPublisher, KafkaEventSeatEventPublisher>();

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<KafkaBookingLifecycleConsumer>();

        return services;
    }
}