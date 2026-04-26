using FluentAssertions;

using RestApiEventProject.Models;
using RestApiEventProject.Services;

namespace RestApiProject.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Should_Create_Pending_Booking_For_Existing_Event()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());

        // Act
        var result = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.EventId.Should().Be(eventItem.Id);
        result.Status.Should().Be(BookingStatus.Pending);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Bookings_With_Unique_Ids_For_Same_Event()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());

        // Act
        var firstBooking = await bookingService.CreateBookingAsync(eventItem.Id);
        var secondBooking = await bookingService.CreateBookingAsync(eventItem.Id);
        var thirdBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        firstBooking.Should().NotBeNull();
        secondBooking.Should().NotBeNull();
        thirdBooking.Should().NotBeNull();

        var ids = new[]
        {
            firstBooking!.Id,
            secondBooking!.Id,
            thirdBooking!.Id
        };

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Booking_When_Id_Exists()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());
        var createdBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var result = await bookingService.GetBookingByIdAsync(createdBooking!.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdBooking.Id);
        result.EventId.Should().Be(eventItem.Id);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Confirmed_Status()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());
        var createdBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var confirmResult = await bookingService.ConfirmBookingAsync(createdBooking!.Id);
        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        confirmResult.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Confirmed);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Rejected_Status()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());
        var createdBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var rejectResult = await bookingService.RejectBookingAsync(createdBooking!.Id);
        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        rejectResult.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Rejected);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_Null_When_Event_Does_Not_Exist()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        // Act
        var result = await bookingService.CreateBookingAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_Null_When_Event_Was_Deleted()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());
        eventService.Delete(eventItem.Id);

        // Act
        var result = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        // Act
        var result = await bookingService.GetBookingByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreatePending_Should_Create_Booking_With_Pending_Status()
    {
        // Act
        var booking = Booking.CreatePending(1, 1);

        // Assert
        booking.Id.Should().Be(1);
        booking.EventId.Should().Be(1);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Confirm_Should_Set_Confirmed_Status_And_ProcessedAt()
    {
        // Arrange
        var booking = Booking.CreatePending(1, 1);

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Reject_Should_Set_Rejected_Status_And_ProcessedAt()
    {
        // Arrange
        var booking = Booking.CreatePending(1, 1);

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Not_Create_Id_Collisions_When_Called_In_Parallel()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());

        // Act
        var tasks = Enumerable.Range(0, 1000)
            .Select(_ => bookingService.CreateBookingAsync(eventItem.Id));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotContainNulls();

        var ids = results
            .Select(booking => booking!.Id)
            .ToList();

        ids.Should().HaveCount(1000);
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetPendingBookingsAsync_Should_Return_Only_Pending_Bookings()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(CreateEvent());

        var firstBooking = await bookingService.CreateBookingAsync(eventItem.Id);
        var secondBooking = await bookingService.CreateBookingAsync(eventItem.Id);
        var thirdBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.ConfirmBookingAsync(secondBooking!.Id);
        await bookingService.RejectBookingAsync(thirdBooking!.Id);

        // Act
        var result = await bookingService.GetPendingBookingsAsync();

        // Assert
        result.Should().ContainSingle();
        result.Single().Id.Should().Be(firstBooking!.Id);
        result.Single().Status.Should().Be(BookingStatus.Pending);
    }

    private static Event CreateEvent()
    {
        return new Event
        {
            Title = "Тестовое мероприятие",
            Description = "Описание",
            StartAt = new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 10, 12, 0, 0)
        };
    }
}