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
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        error.Should().BeNull();
        booking.Should().NotBeNull();
        booking!.Id.Should().BeGreaterThan(0);
        booking.EventId.Should().Be(eventItem.Id);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Bookings_With_Unique_Ids_For_Same_Event()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 3));

        // Act
        var firstResult = await bookingService.CreateBookingAsync(eventItem.Id);
        var secondResult = await bookingService.CreateBookingAsync(eventItem.Id);
        var thirdResult = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        firstResult.Error.Should().BeNull();
        secondResult.Error.Should().BeNull();
        thirdResult.Error.Should().BeNull();

        var ids = new[]
        {
            firstResult.Booking!.Id,
            secondResult.Booking!.Id,
            thirdResult.Booking!.Id
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

        var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var result = await bookingService.GetBookingByIdAsync(createdBooking!.Id);

        // Assert
        createError.Should().BeNull();
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

        var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var confirmResult = await bookingService.ConfirmBookingAsync(createdBooking!.Id);
        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        createError.Should().BeNull();
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

        var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var rejectResult = await bookingService.RejectBookingAsync(createdBooking!.Id);
        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        createError.Should().BeNull();
        rejectResult.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Rejected);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventNotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(999);

        // Assert
        booking.Should().BeNull();
        error.Should().Be(BookingCreateError.EventNotFound);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventNotFound_When_Event_Was_Deleted()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent());

        eventService.Delete(eventItem.Id);

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        booking.Should().BeNull();
        error.Should().Be(BookingCreateError.EventNotFound);
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
        var eventItem = eventService.Create(CreateEvent(totalSeats: 10));

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => bookingService.CreateBookingAsync(eventItem.Id)));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().OnlyContain(result => result.Error == null);
        results.Should().OnlyContain(result => result.Booking != null);

        var ids = results
            .Select(result => result.Booking!.Id)
            .ToList();

        ids.Should().HaveCount(10);
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetPendingBookingsAsync_Should_Return_Only_Pending_Bookings()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 3));

        var firstResult = await bookingService.CreateBookingAsync(eventItem.Id);
        var secondResult = await bookingService.CreateBookingAsync(eventItem.Id);
        var thirdResult = await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.ConfirmBookingAsync(secondResult.Booking!.Id);
        await bookingService.RejectBookingAsync(thirdResult.Booking!.Id);

        // Act
        var result = await bookingService.GetPendingBookingsAsync();

        // Assert
        firstResult.Error.Should().BeNull();
        secondResult.Error.Should().BeNull();
        thirdResult.Error.Should().BeNull();

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(firstResult.Booking!.Id);
        result.Single().Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Decrease_AvailableSeats_By_One()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 3));

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        error.Should().BeNull();
        booking.Should().NotBeNull();
        eventItem.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_NoAvailableSeats_When_Seats_Are_Over()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 1));

        await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        booking.Should().BeNull();
        error.Should().Be(BookingCreateError.NoAvailableSeats);
        eventItem.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task Reject_And_ReleaseSeats_Should_Restore_AvailableSeats()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 1));

        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

        // Act
        await bookingService.RejectBookingAsync(booking!.Id);
        eventItem.ReleaseSeats();

        // Assert
        error.Should().BeNull();
        eventItem.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task Reject_And_ReleaseSeats_Should_Allow_Create_New_Booking()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 1));

        var firstResult = await bookingService.CreateBookingAsync(eventItem.Id);

        await bookingService.RejectBookingAsync(firstResult.Booking!.Id);
        eventItem.ReleaseSeats();

        // Act
        var secondResult = await bookingService.CreateBookingAsync(eventItem.Id);

        // Assert
        firstResult.Error.Should().BeNull();
        secondResult.Error.Should().BeNull();
        secondResult.Booking.Should().NotBeNull();
        secondResult.Booking!.Id.Should().NotBe(firstResult.Booking.Id);
        eventItem.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Prevent_Overbooking_When_Called_In_Parallel()
    {
        // Arrange
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(CreateEvent(totalSeats: 5));

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => bookingService.CreateBookingAsync(eventItem.Id)));

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulBookings = results
            .Where(result => result.Booking is not null)
            .Select(result => result.Booking!)
            .ToList();

        var failedResults = results
            .Where(result => result.Error == BookingCreateError.NoAvailableSeats)
            .ToList();

        successfulBookings.Should().HaveCount(5);
        failedResults.Should().HaveCount(15);
        successfulBookings.Select(booking => booking.Id).Should().OnlyHaveUniqueItems();
        eventItem.AvailableSeats.Should().Be(0);
    }

    private static Event CreateEvent(int totalSeats = 10)
    {
        return new Event
        {
            Title = "Тестовое мероприятие",
            Description = "Описание",
            StartAt = new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 10, 12, 0, 0),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }
}