using BookingsService.Application;
using BookingsService.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace RestApiProject.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Should_Create_Pending_Booking()
    {
        // Arrange
        const int eventId = 1;
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var bookingService =
            scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var (booking, error) =
            await bookingService.CreateBookingAsync(
                eventId,
                userId);

        // Assert
        error.Should().BeNull();

        booking.Should().NotBeNull();
        booking!.Id.Should().BeGreaterThan(0);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);

        booking.CreatedAt.Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Booking_When_Id_Exists()
    {
        // Arrange
        const int eventId = 1;
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService =
                createScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            var (createdBooking, createError) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    userId);

            createError.Should().BeNull();
            bookingId = createdBooking!.Id;
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService =
            checkScope.ServiceProvider
                .GetRequiredService<IBookingService>();

        // Act
        var result =
            await checkBookingService.GetBookingByIdAsync(bookingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookingId);
        result.EventId.Should().Be(eventId);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        using var provider = TestServiceProviderFactory.Create();
        using var scope = provider.CreateScope();

        var bookingService =
            scope.ServiceProvider.GetRequiredService<IBookingService>();

        // Act
        var result =
            await bookingService.GetBookingByIdAsync(999);

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
        var booking =
            Booking.CreatePending(eventId, userId);

        // Assert
        booking.Id.Should().Be(0);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);

        booking.CreatedAt.Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void TryConfirm_Should_Set_Confirmed_Status_And_ProcessedAt_After_Confirmation_Started()
    {
        // Arrange
        var booking = Booking.CreatePending(1, 1);

        booking.TryStartConfirmation().Should().BeTrue();

        // Act
        var result = booking.TryConfirm();

        // Assert
        result.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();

        booking.ProcessedAt.Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TryReject_Should_Set_Rejected_Status_And_ProcessedAt()
    {
        // Arrange
        var booking = Booking.CreatePending(1, 1);

        // Act
        var result = booking.TryReject();

        // Assert
        result.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();

        booking.ProcessedAt.Should()
            .BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_ActiveBookingsLimitExceeded_When_User_Has_Ten_Active_Bookings()
    {
        // Arrange
        const int eventId = 1;
        const long userId = 1;
        const int activeBookingsLimit = 10;

        using var provider = TestServiceProviderFactory.Create();

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            for (var i = 0; i < activeBookingsLimit; i++)
            {
                var result =
                    await bookingService.CreateBookingAsync(
                        eventId,
                        userId);

                result.Error.Should().BeNull();
                result.Booking.Should().NotBeNull();
            }
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    userId);

            // Assert
            booking.Should().BeNull();

            error.Should()
                .Be(BookingCreateError.ActiveBookingsLimitExceeded);
        }

        using var checkScope = provider.CreateScope();

        var bookingRepository =
            checkScope.ServiceProvider
                .GetRequiredService<IBookingRepository>();

        var activeBookingsCount =
            await bookingRepository
                .GetActiveBookingsCountByUserIdAsync(userId);

        activeBookingsCount.Should().Be(activeBookingsLimit);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Not_Apply_Another_Users_Active_Bookings_Limit()
    {
        // Arrange
        const int eventId = 1;
        const long firstUserId = 1;
        const long secondUserId = 2;
        const int activeBookingsLimit = 10;

        using var provider = TestServiceProviderFactory.Create();

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            for (var i = 0; i < activeBookingsLimit; i++)
            {
                var result =
                    await bookingService.CreateBookingAsync(
                        eventId,
                        firstUserId);

                result.Error.Should().BeNull();
                result.Booking.Should().NotBeNull();
            }
        }

        using (var scope = provider.CreateScope())
        {
            var bookingService =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            // Act
            var (booking, error) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    secondUserId);

            // Assert
            error.Should().BeNull();

            booking.Should().NotBeNull();
            booking!.UserId.Should().Be(secondUserId);
        }

        using var checkScope = provider.CreateScope();

        var bookingRepository =
            checkScope.ServiceProvider
                .GetRequiredService<IBookingRepository>();

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
        const int eventId = 1;
        const long bookingOwnerId = 1;
        const long anotherUserId = 2;

        using var provider = TestServiceProviderFactory.Create();

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService =
                createScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    bookingOwnerId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using var cancelScope = provider.CreateScope();

        var cancelBookingService =
            cancelScope.ServiceProvider
                .GetRequiredService<IBookingService>();

        // Act
        var error =
            await cancelBookingService.CancelBookingAsync(
                bookingId,
                anotherUserId,
                isAdmin: false);

        // Assert
        error.Should().Be(BookingCancelError.Forbidden);

        var storedBooking =
            await cancelBookingService
                .GetBookingByIdAsync(bookingId);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Another_Users_Booking_When_User_Is_Admin()
    {
        // Arrange
        const int eventId = 1;
        const long bookingOwnerId = 1;
        const long adminId = 2;

        using var provider = TestServiceProviderFactory.Create();

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService =
                createScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    bookingOwnerId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using (var cancelScope = provider.CreateScope())
        {
            var bookingService =
                cancelScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            // Act
            var error =
                await bookingService.CancelBookingAsync(
                    bookingId,
                    adminId,
                    isAdmin: true);

            // Assert
            error.Should().BeNull();
        }

        using var checkScope = provider.CreateScope();

        var checkBookingService =
            checkScope.ServiceProvider
                .GetRequiredService<IBookingService>();

        var storedBooking =
            await checkBookingService
                .GetBookingByIdAsync(bookingId);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Own_Booking_When_User_Is_Not_Admin()
    {
        // Arrange
        const int eventId = 1;
        const long userId = 1;

        using var provider = TestServiceProviderFactory.Create();

        long bookingId;

        using (var createScope = provider.CreateScope())
        {
            var bookingService =
                createScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            var (booking, createError) =
                await bookingService.CreateBookingAsync(
                    eventId,
                    userId);

            createError.Should().BeNull();
            booking.Should().NotBeNull();

            bookingId = booking!.Id;
        }

        using var cancelScope = provider.CreateScope();

        var cancelBookingService =
            cancelScope.ServiceProvider
                .GetRequiredService<IBookingService>();

        // Act
        var error =
            await cancelBookingService.CancelBookingAsync(
                bookingId,
                userId,
                isAdmin: false);

        // Assert
        error.Should().BeNull();

        var storedBooking =
            await cancelBookingService
                .GetBookingByIdAsync(bookingId);

        storedBooking.Should().NotBeNull();
        storedBooking!.Status.Should().Be(BookingStatus.Cancelled);
    }
}