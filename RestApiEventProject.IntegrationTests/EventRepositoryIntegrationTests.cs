using RestApiEventProject.DataAccess.Repositories;
using RestApiEventProject.IntegrationTests.Infrastructure;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;

namespace RestApiEventProject.IntegrationTests;

public class EventRepositoryIntegrationTests : IntegrationTestBase
{
    public EventRepositoryIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_Should_Save_Event_To_PostgreSql()
    {
        
        await using var context = Fixture.CreateDbContext();
        {
            // Arrange
            var repository = new EventRepository(context);

            var eventItem = CreateEvent(
                id: 1,
                title: "Деловое интеграционно мероприятие аналитики",
                description: "Сокращенно - ДИМА",
                startAt: new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
                endAt: new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc));


            // Act
            await repository.AddAsync(eventItem);
            await repository.SaveChangesAsync();
        }

        await using (var assertContext = Fixture.CreateDbContext())
        {
            // Assert
            var assertRepository = new EventRepository(assertContext);
            var savedEvent = await assertRepository.GetByIdAsync(1);

            Assert.NotNull(savedEvent);
            Assert.Equal(1, savedEvent.Id);
            Assert.Equal("Деловое интеграционно мероприятие аналитики", savedEvent.Title);
            Assert.Equal("Сокращенно - ДИМА", savedEvent.Description);
            Assert.Equal(10, savedEvent.TotalSeats);
            Assert.Equal(10, savedEvent.AvailableSeats);
        }
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Event_Does_Not_Exist()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastIdAsync_Should_Return_Max_Event_Id()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await seedRepository.AddAsync(CreateEvent(id: 1, title: "Первое"));
            await seedRepository.AddAsync(CreateEvent(id: 5, title: "Пятое"));
            await seedRepository.AddAsync(CreateEvent(id: 3, title: "Третье"));
            await seedRepository.SaveChangesAsync();
        }

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetLastIdAsync();

            // Assert
            Assert.Equal(5, result);
        }
    }

    [Fact]
    public async Task GetLastIdAsync_Should_Return_Zero_When_Events_Are_Empty()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetLastIdAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Events_When_No_Filters_Are_Passed()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(5, result.CurrentItemCount);
            Assert.Equal(5, result.Items.Count());
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Title_Case_Insensitive()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Title = "делат", //Почему бы не оставить те же примеры, что и в юнитах
            Page = 1,
            PageSize = 10
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.All(result.Items, eventItem =>
                Assert.Contains("делат", eventItem.Title, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_From_Date()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            From = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, eventItem =>
                Assert.True(eventItem.StartAt >= query.From.Value));
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_To_Date()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            To = new DateTime(2026, 4, 12, 23, 59, 59, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.All(result.Items, eventItem =>
                Assert.True(eventItem.EndAt <= query.To.Value));
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Date_Range()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            From = new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 4, 12, 23, 59, 59, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, eventItem =>
                Assert.True(eventItem.StartAt >= query.From.Value && eventItem.EndAt <= query.To.Value));
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Correct_Page_When_Pagination_Is_Applied()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Page = 2,
            PageSize = 2
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.CurrentItemCount);
            Assert.Equal(2, result.Items.Count());

            var ids = result.Items.Select(eventItem => eventItem.Id).ToArray();

            Assert.Equal([3, 4], ids);
        }
    }

    [Fact]
    public async Task GetAllAsync_Should_Apply_Combined_Filters_And_Pagination()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            await FillEventsAsync(seedRepository);
        }

        var query = new GetEventsQuery
        {
            Title = "делат",
            From = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 5, 20, 23, 59, 59, DateTimeKind.Utc),
            Page = 1,
            PageSize = 1
        };

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(queryContext);

            var result = await repository.GetAllAsync(query);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(1, result.CurrentItemCount);
            Assert.Single(result.Items);

            Assert.Equal("Турник делат", result.Items.Single().Title);
        }
    }

    [Fact]
    public async Task Delete_Should_Remove_Event_From_PostgreSql()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var seedRepository = new EventRepository(seedContext);

            var eventItem = CreateEvent(id: 1, title: "На удаление");

            await seedRepository.AddAsync(eventItem);
            await seedRepository.SaveChangesAsync();
        }

        // Act
        await using (var actContext = Fixture.CreateDbContext())
        {
            var repository = new EventRepository(actContext);

            var eventItem = await repository.GetByIdAsync(1);

            Assert.NotNull(eventItem);

            repository.Delete(eventItem);
            await repository.SaveChangesAsync();
        }

        // Assert
        await using (var assertContext = Fixture.CreateDbContext()) //Хз, нужно ли три разных контекста, или хватит двух, но допустим перестрахуюсь
        {
            var assertRepository = new EventRepository(assertContext);

            var result = await assertRepository.GetByIdAsync(1);

            Assert.Null(result);
        }
    }

    private static async Task FillEventsAsync(EventRepository repository)
    {
        await repository.AddAsync(CreateEvent(
            id: 1,
            title: "Пресс качат",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc)));

        await repository.AddAsync(CreateEvent(
            id: 2,
            title: "10 км бегит",
            startAt: new DateTime(2026, 4, 11, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 4, 11, 12, 0, 0, DateTimeKind.Utc),
            totalSeats: 15));

        await repository.AddAsync(CreateEvent(
            id: 3,
            title: "Турник делат",
            startAt: new DateTime(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc)));

        await repository.AddAsync(CreateEvent(
            id: 4,
            title: "Анжуманя делат",
            startAt: new DateTime(2026, 5, 13, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc),
            totalSeats: 20));

        await repository.AddAsync(CreateEvent(
            id: 5,
            title: "Словарь купит",
            startAt: new DateTime(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc)));

        await repository.SaveChangesAsync();
    }

    private static Event CreateEvent(
        int id,
        string title,
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10,
        string? description = null)
    {
        var eventItem = new Event(
            title,
            description,
            startAt ?? new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            endAt ?? new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            totalSeats);

        eventItem.Id = id;

        return eventItem;
    }
}