using Microsoft.Extensions.DependencyInjection;

namespace EventsService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddSingleton<IEventMapper, EventMapper>();

        return services;
    }
}