using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.DataAccess;
using RestApiEventProject.Services;

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

        services.AddScoped<BookingService>();
        services.AddScoped<IBookingService>(provider =>
            provider.GetRequiredService<BookingService>());
        services.AddScoped<IBookingProcessingService>(provider =>
            provider.GetRequiredService<BookingService>());

        return services.BuildServiceProvider();
    }
}
