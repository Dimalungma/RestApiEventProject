using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.DataAccess.Repositories;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;
using RestApiEventProject.Services;

namespace RestApiProject.Tests;

public class EventServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Assign_Id_And_Return_Created_Event()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var eventItem = CreateEvent(
            title: "Встреча по unit-тестам",
            startAt: UtcDate(2026, 4, 10, 10),
            endAt: UtcDate(2026, 4, 10, 12));

        // Act
        var result = await service.CreateAsync(eventItem);

        // Assert
        result.Id.Should().Be(1);
        result.Title.Should().Be("Встреча по unit-тестам");
        result.StartAt.Should().Be(UtcDate(2026, 4, 10, 10));
        result.EndAt.Should().Be(UtcDate(2026, 4, 10, 12));
        result.TotalSeats.Should().Be(10);
        result.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Events_When_No_Filters_Are_Passed()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await FillEventsAsync(service);

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(query);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.CurrentItemCount.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Event_When_Id_Exists()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(CreateEvent(
            title: "Разработка",
            startAt: UtcDate(2026, 4, 11, 9),
            endAt: UtcDate(2026, 4, 11, 18)));

        // Act
        var result = await service.GetByIdAsync(createdEvent.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdEvent.Id);
        result.Title.Should().Be("Разработка");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Id_Does_Not_Exist() //Так как обращение к несуществующему ID не вызывает у меня exception, то проверка на null
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Existing_Event_And_Return_Success()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var oldEvent = await service.CreateAsync(CreateEvent(
            title: "Старый",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 11)));

        var updatedEvent = CreateEvent(
            title: "Новый",
            description: "С описанием",
            startAt: UtcDate(2026, 4, 12, 12),
            endAt: UtcDate(2026, 4, 12, 13, 30),
            totalSeats: 20);

        // Act
        var updateResult = await service.UpdateAsync(oldEvent.Id, updatedEvent);
        var storedEvent = await service.GetByIdAsync(oldEvent.Id);

        // Assert
        updateResult.Should().Be(EventUpdateResult.Success);

        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be("Новый");
        storedEvent.Description.Should().Be("С описанием");
        storedEvent.StartAt.Should().Be(UtcDate(2026, 4, 12, 12));
        storedEvent.EndAt.Should().Be(UtcDate(2026, 4, 12, 13, 30));
        storedEvent.TotalSeats.Should().Be(20);
        storedEvent.AvailableSeats.Should().Be(20);
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_NotFound_When_Id_Does_Not_Exist()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var updatedEvent = CreateEvent(
            title: "НИИ ЧАВО",
            startAt: UtcDate(2026, 4, 12, 12),
            endAt: UtcDate(2026, 4, 12, 13));

        // Act
        var result = await service.UpdateAsync(999, updatedEvent);

        // Assert
        result.Should().Be(EventUpdateResult.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_Should_Increase_AvailableSeats_When_TotalSeats_Increased()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var eventItem = await service.CreateAsync(CreateEvent(
            title: "Событие",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 11),
            totalSeats: 40));

        eventItem.TryReserveSeats(20);
        await eventRepository.SaveChangesAsync();

        var updatedEvent = CreateEvent(
            title: "Событие обновлено",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 11),
            totalSeats: 50);

        // Act
        var updateResult = await service.UpdateAsync(eventItem.Id, updatedEvent);
        var storedEvent = await service.GetByIdAsync(eventItem.Id);

        // Assert
        updateResult.Should().Be(EventUpdateResult.Success);

        storedEvent.Should().NotBeNull();
        storedEvent!.TotalSeats.Should().Be(50);
        storedEvent.AvailableSeats.Should().Be(30);
    }

    [Fact]
    public async Task UpdateAsync_Should_Decrease_AvailableSeats_When_TotalSeats_Decreased()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var eventItem = await service.CreateAsync(CreateEvent(
            title: "Событие",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 11),
            totalSeats: 40));

        eventItem.TryReserveSeats(20);
        await eventRepository.SaveChangesAsync();

        var updatedEvent = CreateEvent(
            title: "Событие обновлено",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 11),
            totalSeats: 30);

        // Act
        var updateResult = await service.UpdateAsync(eventItem.Id, updatedEvent);
        var storedEvent = await service.GetByIdAsync(eventItem.Id);

        // Assert
        updateResult.Should().Be(EventUpdateResult.Success);

        storedEvent.Should().NotBeNull();
        storedEvent!.TotalSeats.Should().Be(30);
        storedEvent.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_TotalSeatsLessThanReserved_When_New_TotalSeats_Is_Less_Than_Reserved_Seats()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        int eventId;

        using (var arrangeScope = provider.CreateScope())
        {
            var service = arrangeScope.ServiceProvider.GetRequiredService<IEventService>();
            var eventRepository = arrangeScope.ServiceProvider.GetRequiredService<IEventRepository>();

            var eventItem = await service.CreateAsync(CreateEvent(
                title: "Событие",
                startAt: UtcDate(2026, 4, 12, 10),
                endAt: UtcDate(2026, 4, 12, 11),
                totalSeats: 40));

            eventItem.TryReserveSeats(20);
            await eventRepository.SaveChangesAsync();

            eventId = eventItem.Id;
        }

        EventUpdateResult updateResult;

        using (var actScope = provider.CreateScope())
        {
            var service = actScope.ServiceProvider.GetRequiredService<IEventService>();

            var updatedEvent = CreateEvent(
                title: "Событие обновлено",
                startAt: UtcDate(2026, 4, 12, 10),
                endAt: UtcDate(2026, 4, 12, 11),
                totalSeats: 10);

            // Act
            updateResult = await service.UpdateAsync(eventId, updatedEvent);
        }

        using (var assertScope = provider.CreateScope())
        {
            var service = assertScope.ServiceProvider.GetRequiredService<IEventService>();

            var storedEvent = await service.GetByIdAsync(eventId);

            // Assert
            updateResult.Should().Be(EventUpdateResult.TotalSeatsLessThanReserved);

            storedEvent.Should().NotBeNull();
            storedEvent!.Title.Should().Be("Событие");
            storedEvent.TotalSeats.Should().Be(40);
            storedEvent.AvailableSeats.Should().Be(20);
        }
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Existing_Event_And_Return_True()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var createdEvent = await service.CreateAsync(CreateEvent(
            title: "На удаление",
            startAt: UtcDate(2026, 4, 13, 10),
            endAt: UtcDate(2026, 4, 13, 11)));

        // Act
        var deleteResult = await service.DeleteAsync(createdEvent.Id);
        var deletedEvent = await service.GetByIdAsync(createdEvent.Id);

        // Assert
        deleteResult.Should().BeTrue();
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_False_When_Id_Does_Not_Exist() //Исключение не бросается, поэтому просто проверяю на false
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        // Act
        var result = await service.DeleteAsync(999);

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
        return new Event(
            title,
            description,
            startAt,
            endAt,
            totalSeats);
    }

    private static async Task FillEventsAsync(IEventService service)
    {
        await service.CreateAsync(CreateEvent(
            title: "Пресс качат",
            startAt: UtcDate(2026, 4, 10, 10),
            endAt: UtcDate(2026, 4, 10, 12)));

        await service.CreateAsync(CreateEvent(
            title: "10 км бегит",
            startAt: UtcDate(2026, 4, 11, 10),
            endAt: UtcDate(2026, 4, 11, 12),
            totalSeats: 15));

        await service.CreateAsync(CreateEvent(
            title: "Турник делат",
            startAt: UtcDate(2026, 4, 12, 10),
            endAt: UtcDate(2026, 4, 12, 12)));

        await service.CreateAsync(CreateEvent(
            title: "Анжуманя делат",
            startAt: UtcDate(2026, 5, 13, 10),
            endAt: UtcDate(2026, 5, 13, 12),
            totalSeats: 20));

        await service.CreateAsync(CreateEvent(
            title: "Словарь купит",
            startAt: UtcDate(2026, 5, 14, 10),
            endAt: UtcDate(2026, 5, 14, 12)));
    }

    private static DateTime UtcDate(
        //По факту он здесь не нужен, но тогда потеряем единообразие с интеграционными, и возможно в будущем добавлю в методы проверки unspecified до сохранения в базу
        int year,
        int month,
        int day,
        int hour = 0,
        int minute = 0,
        int second = 0)
    {
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }
}