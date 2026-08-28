using EventsService.Application;
using EventsService.Domain;
using EventsService.Infrastructure.DataAccess;
using RestApiEventProject.IntegrationTests.Infrastructure;

namespace RestApiEventProject.IntegrationTests;

public class EventRepositoryIntegrationTests
    : IntegrationTestBase
{
    private static readonly DateTime BaseDate =
        DateTime.UtcNow.Date.AddDays(30);

    public EventRepositoryIntegrationTests(
        PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    protected override Task ResetDatabaseAsync()
    {
        return Fixture.ResetEventsDatabaseAsync();
    }

    [Fact]
    public async Task AddAsync_Should_Save_Event_To_PostgreSql()
    {
        int eventId;

        await using (var context =
                     Fixture.CreateEventsDbContext())
        {
            // Arrange
            var repository =
                new EventRepository(context);

            var eventItem = CreateEvent(
                title:
                "Деловое интеграционное мероприятие аналитики",
                description: "Сокращенно - ДИМА",
                startAt: FutureUtcDate(0, 10),
                endAt: FutureUtcDate(0, 12));

            // Act
            await repository.AddAsync(eventItem);
            await repository.SaveChangesAsync();

            eventId = eventItem.Id;
        }

        await using var assertContext =
            Fixture.CreateEventsDbContext();

        // Assert
        var assertRepository =
            new EventRepository(assertContext);

        var savedEvent =
            await assertRepository.GetByIdAsync(eventId);

        Assert.True(eventId > 0);
        Assert.NotNull(savedEvent);

        Assert.Equal(eventId, savedEvent.Id);

        Assert.Equal(
            "Деловое интеграционное мероприятие аналитики",
            savedEvent.Title);

        Assert.Equal(
            "Сокращенно - ДИМА",
            savedEvent.Description);

        Assert.Equal(10, savedEvent.TotalSeats);
        Assert.Equal(10, savedEvent.AvailableSeats);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Event_Does_Not_Exist()
    {
        // Arrange
        await using var context =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Events_When_No_Filters_Are_Passed()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.CurrentItemCount);
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Title_Case_Insensitive()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Title = "делат", //Почему бы не оставить те же примеры, что и в юнитах
            Page = 1,
            PageSize = 10
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());

        Assert.All(
            result.Items,
            eventItem =>
                Assert.Contains(
                    "делат",
                    eventItem.Title,
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_From_Date()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            From = FutureUtcDate(21),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.All(
            result.Items,
            eventItem =>
                Assert.True(
                    eventItem.StartAt >= query.From.Value));
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_To_Date()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            To = FutureUtcDate(2, 23, 59, 59),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(3, result.TotalCount);

        Assert.All(
            result.Items,
            eventItem =>
                Assert.True(
                    eventItem.EndAt <= query.To.Value));
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Date_Range()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            From = FutureUtcDate(1),
            To = FutureUtcDate(2, 23, 59, 59),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);

        Assert.All(
            result.Items,
            eventItem =>
                Assert.True(
                    eventItem.StartAt >= query.From.Value &&
                    eventItem.EndAt <= query.To.Value));
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Correct_Page_When_Pagination_Is_Applied()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Page = 2,
            PageSize = 2
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.CurrentItemCount);
        Assert.Equal(2, result.Items.Count());

        var ids =
            result.Items
                .Select(eventItem => eventItem.Id)
                .ToArray();

        Assert.Equal([3, 4], ids);
    }

    [Fact]
    public async Task GetAllAsync_Should_Apply_Combined_Filters_And_Pagination()
    {
        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Title = "делат",
            From = FutureUtcDate(2),
            To = FutureUtcDate(40, 23, 59, 59),
            Page = 1,
            PageSize = 1
        };

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var repository =
            new EventRepository(queryContext);

        var result =
            await repository.GetAllAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.CurrentItemCount);
        Assert.Single(result.Items);

        Assert.Equal(
            "Турник делат",
            result.Items.Single().Title);
    }

    [Fact]
    public async Task GetTop10Async_Should_Return_Ten_Events_With_Highest_Sold_Percentage()
    {
        // Arrange
        var soldPercentages = new[]
        {
            5,
            95,
            30,
            70,
            40,
            80,
            20,
            100,
            10,
            60,
            90,
            50
        };

        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var repository =
                new EventRepository(seedContext);

            foreach (var soldPercentage
                     in soldPercentages)
            {
                var eventItem = CreateEvent(
                    title: $"Продано {soldPercentage}",
                    totalSeats: 100);

                eventItem.AvailableSeats =
                    100 - soldPercentage;

                await repository.AddAsync(eventItem);
            }

            await repository.SaveChangesAsync();
        }

        // Act
        await using var queryContext =
            Fixture.CreateEventsDbContext();

        var queryRepository =
            new EventRepository(queryContext);

        var result =
            await queryRepository.GetTop10Async();

        // Assert
        Assert.Equal(10, result.Count);

        var actualSoldPercentages =
            result
                .Select(eventItem =>
                    eventItem.TotalSeats -
                    eventItem.AvailableSeats)
                .ToArray();

        Assert.Equal(
            new[]
            {
                100,
                95,
                90,
                80,
                70,
                60,
                50,
                40,
                30,
                20
            },
            actualSoldPercentages);
    }

    [Fact]
    public async Task Delete_Should_Remove_Event_From_PostgreSql()
    {
        int eventId;

        // Arrange
        await using (var seedContext =
                     Fixture.CreateEventsDbContext())
        {
            var seedRepository =
                new EventRepository(seedContext);

            var eventItem =
                CreateEvent(title: "На удаление");

            await seedRepository.AddAsync(eventItem);
            await seedRepository.SaveChangesAsync();

            eventId = eventItem.Id;
        }

        // Act
        await using (var actContext =
                     Fixture.CreateEventsDbContext())
        {
            var repository =
                new EventRepository(actContext);

            var eventItem =
                await repository.GetByIdAsync(eventId);

            Assert.NotNull(eventItem);

            repository.Delete(eventItem);

            await repository.SaveChangesAsync();
        }

        // Assert
        await using var assertContext =
            Fixture.CreateEventsDbContext();

        var assertRepository =
            new EventRepository(assertContext);

        var result =
            await assertRepository.GetByIdAsync(eventId);

        Assert.Null(result);
    }

    private static async Task FillEventsAsync(
        EventRepository repository)
    {
        await repository.AddAsync(
            CreateEvent(
                title: "Пресс качат",
                startAt: FutureUtcDate(0, 10),
                endAt: FutureUtcDate(0, 12)));

        await repository.AddAsync(
            CreateEvent(
                title: "10 км бегит",
                startAt: FutureUtcDate(1, 10),
                endAt: FutureUtcDate(1, 12),
                totalSeats: 15));

        await repository.AddAsync(
            CreateEvent(
                title: "Турник делат",
                startAt: FutureUtcDate(2, 10),
                endAt: FutureUtcDate(2, 12)));

        await repository.AddAsync(
            CreateEvent(
                title: "Анжуманя делат",
                startAt: FutureUtcDate(33, 10),
                endAt: FutureUtcDate(33, 12),
                totalSeats: 20));

        await repository.AddAsync(
            CreateEvent(
                title: "Словарь купит",
                startAt: FutureUtcDate(34, 10),
                endAt: FutureUtcDate(34, 12)));

        await repository.SaveChangesAsync();
    }

    private static Event CreateEvent(
        string title,
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10,
        string? description = null)
    {
        return new Event(
            title,
            description,
            startAt ?? FutureUtcDate(0, 10),
            endAt ?? FutureUtcDate(0, 12),
            totalSeats);
    }

    private static DateTime FutureUtcDate(
        int daysFromBase,
        int hour = 0,
        int minute = 0,
        int second = 0)
    {
        return BaseDate
            .AddDays(daysFromBase)
            .AddHours(hour)
            .AddMinutes(minute)
            .AddSeconds(second);
    }
}