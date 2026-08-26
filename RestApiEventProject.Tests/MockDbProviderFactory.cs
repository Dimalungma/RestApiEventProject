using BookingsService.Application;
using EventsService.Application;
using Microsoft.Extensions.DependencyInjection;
using RestApiProject.Tests.Infrastructure;
using UsersService.Application;

namespace RestApiProject.Tests;

internal static class TestServiceProviderFactory //MockDbProviderFactory
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();

        // Events
        services.AddSingleton<TestEventStore>();
        services.AddScoped<IEventRepository, TestEventRepository>();

        services.AddSingleton<ICacheService, TestCacheService>();
        services.AddSingleton(new CacheOptions
        {
            EventTtlMinutes = 5,
            TopEventsTtlMinutes = 1
        });

        services.AddScoped<IEventService, EventService>();

        // Bookings
        services.AddSingleton<TestBookingStore>();
        services.AddScoped<IBookingRepository, TestBookingRepository>();

        services.AddSingleton<IBookingEventPublisher, TestBookingEventPublisher>();
        services.AddSingleton<IUserBookingLock, TestUserBookingLock>();

        services.AddScoped<IBookingService, BookingService>();

        // Users
        services.AddSingleton<TestUserStore>();
        services.AddScoped<IUserRepository, TestUserRepository>();

        services.AddSingleton<IPasswordHasher, TestPasswordHasher>();

        services.AddSingleton<TestJwtTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator>(
            provider => provider.GetRequiredService<TestJwtTokenGenerator>());

        services.AddScoped<IUserService, UserService>();

        return services.BuildServiceProvider();
    }
}