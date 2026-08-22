using Microsoft.Extensions.DependencyInjection;

namespace EventsService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>(); 
        services.AddScoped<IBookingLifecycleHandler, BookingLifecycleHandler>();

        services.AddSingleton<IEventMapper, EventMapper>();

        return services;
    }
}