using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using BookingsDbContext = BookingsService.Infrastructure.DataAccess.AppDbContext;
using EventsDbContext = EventsService.Infrastructure.DataAccess.AppDbContext;
using UsersDbContext = UsersService.Infrastructure.DataAccess.AppDbContext;

namespace RestApiEventProject.IntegrationTests.Infrastructure;

public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _eventsPostgresContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("eventapi_events_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private readonly PostgreSqlContainer _bookingsPostgresContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("eventapi_bookings_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private readonly PostgreSqlContainer _usersPostgresContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("eventapi_users_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        await _eventsPostgresContainer.StartAsync();
        await _bookingsPostgresContainer.StartAsync();
        await _usersPostgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _eventsPostgresContainer.DisposeAsync();
        await _bookingsPostgresContainer.DisposeAsync();
        await _usersPostgresContainer.DisposeAsync();
    }

    public EventsDbContext CreateEventsDbContext()
    {
        var options =
            new DbContextOptionsBuilder<EventsDbContext>()
                .UseNpgsql(
                    _eventsPostgresContainer.GetConnectionString())
                .Options;

        return new EventsDbContext(options);
    }

    public BookingsDbContext CreateBookingsDbContext()
    {
        var options =
            new DbContextOptionsBuilder<BookingsDbContext>()
                .UseNpgsql(
                    _bookingsPostgresContainer.GetConnectionString())
                .Options;

        return new BookingsDbContext(options);
    }

    public UsersDbContext CreateUsersDbContext()
    {
        var options =
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseNpgsql(
                    _usersPostgresContainer.GetConnectionString())
                .Options;

        return new UsersDbContext(options);
    }

    public async Task ResetEventsDatabaseAsync()
    {
        await using var context = CreateEventsDbContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task ResetBookingsDatabaseAsync()
    {
        await using var context = CreateBookingsDbContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task ResetUsersDatabaseAsync()
    {
        await using var context = CreateUsersDbContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}