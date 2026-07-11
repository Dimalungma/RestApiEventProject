using Microsoft.Extensions.DependencyInjection;

namespace RestApiEventProject.Application;
//Не уверен, может и нормально оставить все в Program.cs, но по идее он часть presentation и не должен отвечать за связывание классов, целиком хранящихся в Application'е
public static class DependencyInjection 
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessingService, BookingProcessingService>();
        services.AddScoped<IUserService, UserService>();

        services.AddSingleton<IEventMapper, EventMapper>();
        services.AddSingleton<IBookingMapper, BookingMapper>();

        return services;
    }
}