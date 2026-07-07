using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.Application;
using RestApiEventProject.Infrastructure.DataAccess;

namespace RestApiProject.Tests;

internal static class TestServiceProviderFactory //MockDbProviderFactory
{
    public static ServiceProvider Create()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services.BuildServiceProvider();
    }
}
