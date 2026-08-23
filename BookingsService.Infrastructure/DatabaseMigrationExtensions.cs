using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingsService.Infrastructure.DataAccess;

namespace BookingsService.Infrastructure;

public static class DatabaseMigrationExtensions
{
    public static void ApplyDatabaseMigrations(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.Migrate();
    }
}