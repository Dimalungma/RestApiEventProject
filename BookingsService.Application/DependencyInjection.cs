using Microsoft.Extensions.DependencyInjection;

namespace BookingsService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessingService, BookingProcessingService>();
        services.AddSingleton<IBookingMapper, BookingMapper>();

        return services;
    }
}