using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.Models;
using RestApiEventProject.Services;

namespace RestApiProject.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Should_Create_Pending_Booking_For_Existing_Event()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider);

        using var scope = provider.CreateScope();

        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

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
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        using var scope = provider.CreateScope();

        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

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
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider);

        long bookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

            createError.Should().BeNull();

            bookingId = createdBooking!.Id;
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService = checkScope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var result = await checkBookingService.GetBookingByIdAsync(bookingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookingId);
        result.EventId.Should().Be(eventItem.Id);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Confirmed_Status()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider);

        long bookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

            createError.Should().BeNull();

            bookingId = createdBooking!.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

            // Act
            var confirmResult = await bookingProcessingService.ConfirmBookingAsync(bookingId);

            // Assert
            confirmResult.Should().BeTrue();
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService = checkScope.ServiceProvider.GetRequiredService<IBookingService>();

        var result = await checkBookingService.GetBookingByIdAsync(bookingId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Confirmed);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Rejected_Status()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider);

        long bookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id);

            createError.Should().BeNull();

            bookingId = createdBooking!.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

            // Act
            var rejectResult = await bookingProcessingService.RejectBookingAsync(bookingId);

            // Assert
            rejectResult.Should().BeTrue();
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService = checkScope.ServiceProvider.GetRequiredService<IBookingService>();

        var result = await checkBookingService.GetBookingByIdAsync(bookingId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Rejected);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventNotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

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
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await eventService.DeleteAsync(eventItem.Id);
        }

        using var bookingScope = provider.CreateScope();

        var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

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
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

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
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();

                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(eventItem.Id);
            }));

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
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        long firstBookingId;
        long secondBookingId;
        long thirdBookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var firstResult = await bookingService.CreateBookingAsync(eventItem.Id);
            var secondResult = await bookingService.CreateBookingAsync(eventItem.Id);
            var thirdResult = await bookingService.CreateBookingAsync(eventItem.Id);

            firstResult.Error.Should().BeNull();
            secondResult.Error.Should().BeNull();
            thirdResult.Error.Should().BeNull();

            firstBookingId = firstResult.Booking!.Id;
            secondBookingId = secondResult.Booking!.Id;
            thirdBookingId = thirdResult.Booking!.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

            await bookingProcessingService.ConfirmBookingAsync(secondBookingId);
            await bookingProcessingService.RejectBookingAsync(thirdBookingId);
        }

        using var checkScope = provider.CreateScope();

        var checkBookingProcessingService = checkScope.ServiceProvider.GetRequiredService<IBookingProcessingService>();

        // Act
        var result = await checkBookingProcessingService.GetPendingBookingsAsync();

        // Assert
        result.Should().ContainSingle();
        result.Single().Id.Should().Be(firstBookingId);
        result.Single().Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Decrease_AvailableSeats_By_One()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            error.Should().BeNull();
            booking.Should().NotBeNull();
        }

        using var checkScope = provider.CreateScope();

        var eventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

        storedEvent!.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_NoAvailableSeats_When_Seats_Are_Over()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 1);

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await bookingService.CreateBookingAsync(eventItem.Id);
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            booking.Should().BeNull();
            error.Should().Be(BookingCreateError.NoAvailableSeats);
        }

        using var checkScope = provider.CreateScope();

        var eventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

        storedEvent!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task Reject_And_ReleaseSeats_Should_Restore_AvailableSeats()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 1);

        long bookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id);

            error.Should().BeNull();

            bookingId = booking!.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

            // Act
            await bookingProcessingService.RejectBookingAsync(bookingId);
            storedEvent!.ReleaseSeats();
            await eventService.UpdateAsync(storedEvent.Id, storedEvent);
        }

        using var checkScope = provider.CreateScope();

        var checkEventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var updatedEvent = await checkEventService.GetByIdAsync(eventItem.Id);

        // Assert
        updatedEvent!.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task Reject_And_ReleaseSeats_Should_Allow_Create_New_Booking()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 1);

        long firstBookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var firstResult = await bookingService.CreateBookingAsync(eventItem.Id);

            firstResult.Error.Should().BeNull();

            firstBookingId = firstResult.Booking!.Id;
        }

        using (var scope = provider.CreateScope())
        {
            var bookingProcessingService = scope.ServiceProvider.GetRequiredService<IBookingProcessingService>();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

            await bookingProcessingService.RejectBookingAsync(firstBookingId);
            storedEvent!.ReleaseSeats();
            await eventService.UpdateAsync(storedEvent.Id, storedEvent);
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var secondResult = await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            secondResult.Error.Should().BeNull();
            secondResult.Booking.Should().NotBeNull();
            secondResult.Booking!.Id.Should().NotBe(firstBookingId);
        }

        using var checkScope = provider.CreateScope();

        var checkEventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var updatedEvent = await checkEventService.GetByIdAsync(eventItem.Id);

        updatedEvent!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Prevent_Overbooking_When_Called_In_Parallel()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();

                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(eventItem.Id);
            }));

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

        successfulBookings
            .Select(booking => booking.Id)
            .Should()
            .OnlyHaveUniqueItems();

        using var checkScope = provider.CreateScope();

        var eventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

        storedEvent!.AvailableSeats.Should().Be(0);
    }

    private static async Task<Event> CreateStoredEventAsync(ServiceProvider provider, int totalSeats = 10)
    {
        using var scope = provider.CreateScope();

        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        return await eventService.CreateAsync(CreateEvent(totalSeats));
    }

    private static Event CreateEvent(int totalSeats = 10)
    {
        return new Event(
            "Тестовое мероприятие",
            "Описание",
            new DateTime(2026, 4, 10, 10, 0, 0),
            new DateTime(2026, 4, 10, 12, 0, 0),
            totalSeats);
    }
}