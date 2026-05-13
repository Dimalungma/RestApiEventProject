using FluentAssertions;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;
using RestApiEventProject.Services;

namespace RestApiProject.Tests;

public class EventServiceTests
{
    [Fact]
    public void Create_Should_Assign_Id_And_Return_Created_Event()
    {
        // Arrange
        var service = new EventService();
        var eventItem = CreateEvent(
            title: "Встреча по unit-тестам",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0));

        // Act
        var result = service.Create(eventItem);

        // Assert
        result.Id.Should().Be(1);
        result.Title.Should().Be("Встреча по unit-тестам");
        result.StartAt.Should().Be(new DateTime(2026, 4, 10, 10, 0, 0));
        result.EndAt.Should().Be(new DateTime(2026, 4, 10, 12, 0, 0));
    }

    [Fact]
    public void GetAll_Should_Return_All_Events_When_No_Filters_Are_Passed()
    {
        // Arrange
        var service = new EventService();
        FillEvents(service);

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = service.GetAll(query);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.CurrentItemCount.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public void GetById_Should_Return_Event_When_Id_Exists()
    {
        // Arrange
        var service = new EventService();
        var createdEvent = service.Create(CreateEvent(
            title: "Разработка",
            startAt: new DateTime(2026, 4, 11, 9, 0, 0),
            endAt: new DateTime(2026, 4, 11, 18, 0, 0)));

        // Act
        var result = service.GetById(createdEvent.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdEvent.Id);
        result.Title.Should().Be("Разработка");
    }

    [Fact]
    public void GetById_Should_Return_Null_When_Id_Does_Not_Exist() //Так как обращение к несуществующему ID не вызывает у меня exception, то проверка на null
    {
        // Arrange
        var service = new EventService();

        // Act
        var result = service.GetById(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Update_Should_Update_Existing_Event_And_Return_True()
    {
        // Arrange
        var service = new EventService();
        var oldEvent = service.Create(CreateEvent(
            title: "Старый",
            startAt: new DateTime(2026, 4, 12, 10, 0, 0),
            endAt: new DateTime(2026, 4, 12, 11, 0, 0)));

        var updatedEvent = CreateEvent(
            title: "Новый",
            description: "С описанием",
            startAt: new DateTime(2026, 4, 12, 12, 0, 0),
            endAt: new DateTime(2026, 4, 12, 13, 30, 0));

        // Act
        var updateResult = service.Update(oldEvent.Id, updatedEvent);
        var storedEvent = service.GetById(oldEvent.Id);

        // Assert
        updateResult.Should().BeTrue();
        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be("Новый");
        storedEvent.Description.Should().Be("С описанием");
        storedEvent.StartAt.Should().Be(new DateTime(2026, 4, 12, 12, 0, 0));
        storedEvent.EndAt.Should().Be(new DateTime(2026, 4, 12, 13, 30, 0));
    }

    [Fact]
    public void Update_Should_Return_False_When_Id_Does_Not_Exist()
    {
        // Arrange
        var service = new EventService();
        var updatedEvent = CreateEvent(
            title: "НИИ ЧАВО",
            startAt: new DateTime(2026, 4, 12, 12, 0, 0),
            endAt: new DateTime(2026, 4, 12, 13, 0, 0));

        // Act
        var result = service.Update(999, updatedEvent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Delete_Should_Remove_Existing_Event_And_Return_True()
    {
        // Arrange
        var service = new EventService();
        var createdEvent = service.Create(CreateEvent(
            title: "На удаление",
            startAt: new DateTime(2026, 4, 13, 10, 0, 0),
            endAt: new DateTime(2026, 4, 13, 11, 0, 0)));

        // Act
        var deleteResult = service.Delete(createdEvent.Id);
        var deletedEvent = service.GetById(createdEvent.Id);

        // Assert
        deleteResult.Should().BeTrue();
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public void Filter_By_Title_Should_Return_Only_Matching_Events()
    {
        // Arrange
        var service = new EventService();
        FillEvents(service);

        var query = new GetEventsQuery
        {
            Title = "делат",
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = service.GetAll(query);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(e => e.Title).Should().OnlyContain(title =>
            title.Contains("делат", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filter_By_Dates_Should_Return_Only_Events_Inside_Range()
    {
        // Arrange
        var service = new EventService();
        FillEvents(service);

        var query = new GetEventsQuery
        {
            From = new DateTime(2026, 4, 10, 0, 0, 0),
            To = new DateTime(2026, 4, 16, 23, 59, 59),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = service.GetAll(query);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().OnlyContain(e =>
            e.StartAt >= query.From!.Value &&
            e.EndAt <= query.To!.Value);
    }

    [Fact]
    public void GetAll_Should_Return_Correct_Page_When_Pagination_Is_Applied()
    {
        // Arrange
        var service = new EventService();
        FillEvents(service);

        var query = new GetEventsQuery
        {
            Page = 2,
            PageSize = 2
        };

        // Act
        var result = service.GetAll(query);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.CurrentItemCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(e => e.Id).Should().Equal(3, 4);
    }

    [Fact]
    public void GetAll_Should_Apply_Combined_Filters_And_Pagination()
    {
        // Arrange
        var service = new EventService();
        FillEvents(service);

        var query = new GetEventsQuery
        {
            Title = "делат",
            From = new DateTime(2026, 4, 12, 0, 0, 0),
            To = new DateTime(2026, 4, 20, 23, 59, 59),
            Page = 1,
            PageSize = 1
        };

        // Act
        var result = service.GetAll(query);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.CurrentItemCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Title.Should().Be("Турник делат");
    }

    [Fact]
    public void Delete_Should_Return_False_When_Id_Does_Not_Exist() //Исключение не бросается, поэтому просто проверяю на false
    {
        // Arrange
        var service = new EventService();

        // Act
        var result = service.Delete(999);

        // Assert
        result.Should().BeFalse();
    }

    private static Event CreateEvent(
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats = 10,
        string? description = null)
    {
        return new Event
        {
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    private static void FillEvents(EventService service)
    {
        service.Create(CreateEvent(
            title: "Пресс качат",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0)));

        service.Create(CreateEvent(
            title: "10 км бегит",
            startAt: new DateTime(2026, 4, 11, 10, 0, 0),
            endAt: new DateTime(2026, 4, 11, 12, 0, 0),
            totalSeats: 15));

        service.Create(CreateEvent(
            title: "Турник делат",
            startAt: new DateTime(2026, 4, 12, 10, 0, 0),
            endAt: new DateTime(2026, 4, 12, 12, 0, 0)));

        service.Create(CreateEvent(
            title: "Анжуманя делат",
            startAt: new DateTime(2026, 5, 13, 10, 0, 0),
            endAt: new DateTime(2026, 5, 13, 12, 0, 0),
            totalSeats: 20));

        service.Create(CreateEvent(
            title: "Словарь купит",
            startAt: new DateTime(2026, 5, 14, 10, 0, 0),
            endAt: new DateTime(2026, 5, 14, 12, 0, 0)));
    }
}