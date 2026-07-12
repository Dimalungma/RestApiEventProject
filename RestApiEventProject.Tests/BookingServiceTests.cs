using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestApiEventProject.Application;
using RestApiEventProject.Domain;

namespace RestApiProject.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Should_Create_Pending_Booking_For_Existing_Event()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider);

        using var scope = provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id, userId);

        // Assert
        error.Should().BeNull();

        booking.Should().NotBeNull();
        booking!.Id.Should().BeGreaterThan(0);
        booking.EventId.Should().Be(eventItem.Id);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Bookings_With_Unique_Ids_For_Same_Event()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        using var scope = provider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var firstResult = await bookingService.CreateBookingAsync(eventItem.Id, userId);

        var secondResult = await bookingService.CreateBookingAsync(eventItem.Id, userId);

        var thirdResult = await bookingService.CreateBookingAsync(eventItem.Id, userId);

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
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider);

        long bookingId;

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var (createdBooking, createError) = await bookingService.CreateBookingAsync(eventItem.Id, userId);

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
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventNotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var (booking, error) = await bookingService.CreateBookingAsync(999, userId);

        // Assert
        booking.Should().BeNull();
        error.Should().Be(BookingCreateError.EventNotFound);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventNotFound_When_Event_Was_Deleted()
    {
        // Arrange
        const long userId = 1;

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
        var (booking, error) = await bookingService.CreateBookingAsync(eventItem.Id, userId);

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
    public void CreatePending_Should_Create_Booking_With_User_And_Pending_Status()
    {
        // Arrange
        const int eventId = 1;
        const long userId = 2;

        // Act
        var booking = Booking.CreatePending(eventId, userId);

        // Assert
        booking.Id.Should().Be(0);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
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
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 10);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();

                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should()
            .OnlyContain(result => result.Error == null);

        results.Should()
            .OnlyContain(result => result.Booking != null);

        var ids = results
            .Select(result => result.Booking!.Id)
            .ToList();

        ids.Should().HaveCount(10);
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Decrease_AvailableSeats_By_One()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);

            // Assert
            error.Should().BeNull();
            booking.Should().NotBeNull();
        }

        using var checkScope = provider.CreateScope();

        var eventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedEvent =
            await eventService.GetByIdAsync(eventItem.Id);

        storedEvent!.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_NoAvailableSeats_When_Seats_Are_Over()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 1);

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await bookingService.CreateBookingAsync(
                eventItem.Id,
                userId);
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);

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
    public async Task CreateBookingAsync_Should_Prevent_Overbooking_When_Called_In_Parallel()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();

        var eventItem =
            await CreateStoredEventAsync(provider, totalSeats: 5);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();

                var bookingService =
                    scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulBookings = results
            .Where(result => result.Booking is not null)
            .Select(result => result.Booking!)
            .ToList();

        var failedResults = results
            .Where(result =>
                result.Error == BookingCreateError.NoAvailableSeats)
            .ToList();

        successfulBookings.Should().HaveCount(5);
        failedResults.Should().HaveCount(15);

        successfulBookings
            .Select(booking => booking.Id)
            .Should()
            .OnlyHaveUniqueItems();

        using var checkScope = provider.CreateScope();

        var eventService =
            checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedEvent =
            await eventService.GetByIdAsync(eventItem.Id);

        storedEvent!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_EventAlreadyStarted_When_Event_Is_In_The_Past()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();

        var startAt = DateTime.UtcNow.AddHours(-2);
        var endAt = DateTime.UtcNow.AddHours(-1);

        var eventItem = await CreateStoredEventAsync(
            provider,
            startAt: startAt,
            endAt: endAt);

        using var scope = provider.CreateScope();

        var bookingService =
            scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var (booking, error) =
            await bookingService.CreateBookingAsync(
                eventItem.Id,
                userId);

        // Assert
        booking.Should().BeNull();
        error.Should().Be(BookingCreateError.EventAlreadyStarted);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_ActiveBookingsLimitExceeded_When_User_Has_Ten_Active_Bookings()
    {
        // Arrange
        const long userId = 1;
        const int activeBookingsLimit = 10;

        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(
            provider,
            totalSeats: 15);

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider.GetRequiredService<IBookingService>();

            for (var i = 0; i < activeBookingsLimit; i++)
            {
                var result = await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);

                result.Error.Should().BeNull();
                result.Booking.Should().NotBeNull();
            }
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    userId);

            // Assert
            booking.Should().BeNull();
            error.Should()
                .Be(BookingCreateError.ActiveBookingsLimitExceeded);
        }

        using var checkScope = provider.CreateScope();

        var bookingRepository =
            checkScope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var activeBookingsCount =
            await bookingRepository
                .GetActiveBookingsCountByUserIdAsync(userId);

        activeBookingsCount.Should().Be(activeBookingsLimit);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Not_Apply_Another_Users_Active_Bookings_Limit()
    {
        // Arrange
        const long firstUserId = 1;
        const long secondUserId = 2;
        const int activeBookingsLimit = 10;

        using var provider = TestServiceProviderFactory.Create();

        var eventItem = await CreateStoredEventAsync(
            provider,
            totalSeats: 11);

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider.GetRequiredService<IBookingService>();

            for (var i = 0; i < activeBookingsLimit; i++)
            {
                var result = await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    firstUserId);

                result.Error.Should().BeNull();
                result.Booking.Should().NotBeNull();
            }
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventItem.Id,
                    secondUserId);

            // Assert
            error.Should().BeNull();
            booking.Should().NotBeNull();
            booking!.UserId.Should().Be(secondUserId);
        }

        using var checkScope = provider.CreateScope();

        var bookingRepository =
            checkScope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var firstUserBookings =
            await bookingRepository
                .GetActiveBookingsCountByUserIdAsync(firstUserId);

        var secondUserBookings =
            await bookingRepository
                .GetActiveBookingsCountByUserIdAsync(secondUserId);

        firstUserBookings.Should().Be(activeBookingsLimit);
        secondUserBookings.Should().Be(1);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Return_Forbidden_When_User_Cancels_Another_Users_Booking()
    {
        // Arrange
        const long bookingOwnerId = 1;
        const long anotherUserId = 2;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(eventItem.Id, bookingOwnerId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using var cancelScope = provider.CreateScope();
        var cancelBookingService = cancelScope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var error = await cancelBookingService.CancelBookingAsync(
            bookingId,
            anotherUserId,
            isAdmin: false);

        // Assert
        error.Should().Be(BookingCancelError.Forbidden);

        var storedBooking = await cancelBookingService.GetBookingByIdAsync(bookingId);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Another_Users_Booking_When_User_Is_Admin()
    {
        // Arrange
        const long bookingOwnerId = 1;
        const long adminId = 2;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(eventItem.Id, bookingOwnerId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using (var cancelScope = provider.CreateScope())
        {
            var bookingService = cancelScope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act
            var error = await bookingService.CancelBookingAsync(
                bookingId,
                adminId,
                isAdmin: true);

            // Assert
            error.Should().BeNull();
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService = checkScope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = checkScope.ServiceProvider.GetRequiredService<IEventService>();

        var storedBooking = await checkBookingService.GetBookingByIdAsync(bookingId);
        var storedEvent = await eventService.GetByIdAsync(eventItem.Id);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Cancelled);
        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Own_Booking_When_User_Is_Not_Admin()
    {
        // Arrange
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        var eventItem = await CreateStoredEventAsync(provider, totalSeats: 3);

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService = createScope.ServiceProvider.GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(eventItem.Id, userId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using var cancelScope = provider.CreateScope();
        var cancelBookingService = cancelScope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var error = await cancelBookingService.CancelBookingAsync(
            bookingId,
            userId,
            isAdmin: false);

        // Assert
        error.Should().BeNull();

        var storedBooking = await cancelBookingService.GetBookingByIdAsync(bookingId);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Cancelled);
    }


    private static async Task<Event> CreateStoredEventAsync(
        ServiceProvider provider,
        int totalSeats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        using var scope = provider.CreateScope();

        var eventService =
            scope.ServiceProvider.GetRequiredService<IEventService>();

        return await eventService.CreateAsync(
            CreateEvent(
                totalSeats,
                startAt,
                endAt));
    }

    private static Event CreateEvent(
        int totalSeats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        var actualStartAt =
            startAt ?? DateTime.UtcNow.AddDays(1); //Иначе теперь будут отстреливать проверки на "мероприятие уже началось"

        var actualEndAt =
            endAt ?? actualStartAt.AddHours(2);

        return new Event(
            "Тестовое мероприятие",
            "Описание",
            actualStartAt,
            actualEndAt,
            totalSeats);
    }
}