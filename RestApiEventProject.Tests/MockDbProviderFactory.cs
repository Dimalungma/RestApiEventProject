using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.Application;
using RestApiEventProject.Infrastructure.DataAccess;
using RestApiEventProject.Infrastructure.Security;
using RestApiProject.Tests.Infrastructure;

namespace RestApiProject.Tests;

internal static class TestServiceProviderFactory //MockDbProviderFactory
{
    public static ServiceProvider Create()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(
            options => options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserService, UserService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<TestJwtTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator>(
            provider => provider.GetRequiredService<TestJwtTokenGenerator>());

        return services.BuildServiceProvider();
    }
}