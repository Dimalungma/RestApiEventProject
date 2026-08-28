using BookingsService.Domain;
using BookingsService.Infrastructure.DataAccess;
using RestApiEventProject.IntegrationTests.Infrastructure;

namespace RestApiEventProject.IntegrationTests;

public class BookingRepositoryIntegrationTests
    : IntegrationTestBase
{
    public BookingRepositoryIntegrationTests(
        PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    protected override Task ResetDatabaseAsync()
    {
        return Fixture.ResetBookingsDatabaseAsync();
    }

    [Fact]
    public async Task AddAsync_Should_Save_Booking_To_PostgreSql()
    {
        long bookingId;

        const int eventId = 100;
        const long userId = 200;

        // Arrange
        await using (var context =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(context);

            var booking =
                Booking.CreatePending(
                    eventId,
                    userId);

            // Act
            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();

            bookingId = booking.Id;
        }

        // Assert
        await using var assertContext =
            Fixture.CreateBookingsDbContext();

        var assertRepository =
            new BookingRepository(assertContext);

        var savedBooking =
            await assertRepository.GetByIdAsync(bookingId);

        Assert.True(bookingId > 0);
        Assert.NotNull(savedBooking);

        Assert.Equal(
            bookingId,
            savedBooking.Id);

        Assert.Equal(
            eventId,
            savedBooking.EventId);

        Assert.Equal(
            userId,
            savedBooking.UserId);

        Assert.Equal(
            BookingStatus.Pending,
            savedBooking.Status);

        Assert.Null(savedBooking.ProcessedAt);

        Assert.Null(
            savedBooking.ConfirmationRequestedAt);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Booking_Does_Not_Exist()
    {
        // Arrange
        await using var context =
            Fixture.CreateBookingsDbContext();

        var repository =
            new BookingRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_Should_Return_Only_Pending_Booking_Ids()
    {
        long pendingBookingId;

        // Arrange
        await using (var seedContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(seedContext);

            var pendingBooking =
                Booking.CreatePending(1, 1);

            var awaitingBooking =
                Booking.CreatePending(2, 1);

            var rejectedBooking =
                Booking.CreatePending(3, 1);

            Assert.True(
                awaitingBooking.TryStartConfirmation());

            Assert.True(
                rejectedBooking.TryReject());

            await repository.AddAsync(pendingBooking);
            await repository.AddAsync(awaitingBooking);
            await repository.AddAsync(rejectedBooking);

            await repository.SaveChangesAsync();

            pendingBookingId = pendingBooking.Id;
        }

        // Act
        await using var queryContext =
            Fixture.CreateBookingsDbContext();

        var queryRepository =
            new BookingRepository(queryContext);

        var result =
            await queryRepository
                .GetPendingBookingIdsAsync();

        // Assert
        Assert.Single(result);

        Assert.Equal(
            pendingBookingId,
            result.Single());
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Confirmed_Status()
    {
        long bookingId;

        // Arrange
        await using (var seedContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(seedContext);

            var booking =
                Booking.CreatePending(1, 1);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();

            bookingId = booking.Id;
        }

        // Act
        await using (var actContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(actContext);

            var booking =
                await repository.GetByIdAsync(bookingId);

            Assert.NotNull(booking);

            Assert.True(
                booking.TryStartConfirmation());

            Assert.True(
                booking.TryConfirm());

            await repository.SaveChangesAsync();
        }

        // Assert
        await using var assertContext =
            Fixture.CreateBookingsDbContext();

        var assertRepository =
            new BookingRepository(assertContext);

        var savedBooking =
            await assertRepository.GetByIdAsync(bookingId);

        Assert.NotNull(savedBooking);

        Assert.Equal(
            BookingStatus.Confirmed,
            savedBooking.Status);

        Assert.NotNull(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Rejected_Status()
    {
        long bookingId;

        // Arrange
        await using (var seedContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(seedContext);

            var booking =
                Booking.CreatePending(1, 1);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();

            bookingId = booking.Id;
        }

        // Act
        await using (var actContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(actContext);

            var booking =
                await repository.GetByIdAsync(bookingId);

            Assert.NotNull(booking);

            Assert.True(
                booking.TryReject());

            await repository.SaveChangesAsync();
        }

        // Assert
        await using var assertContext =
            Fixture.CreateBookingsDbContext();

        var assertRepository =
            new BookingRepository(assertContext);

        var savedBooking =
            await assertRepository.GetByIdAsync(bookingId);

        Assert.NotNull(savedBooking);

        Assert.Equal(
            BookingStatus.Rejected,
            savedBooking.Status);

        Assert.NotNull(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task AddAsync_Should_Not_Require_Event_Or_User_In_Bookings_Database()
    {
        // Arrange
        const int externalEventId = 999;
        const long externalUserId = 777;

        await using var context =
            Fixture.CreateBookingsDbContext();

        var repository =
            new BookingRepository(context);

        var booking =
            Booking.CreatePending(
                externalEventId,
                externalUserId);

        // Act
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();

        // Assert
        Assert.True(booking.Id > 0);

        var savedBooking =
            await repository.GetByIdAsync(booking.Id);

        Assert.NotNull(savedBooking);

        Assert.Equal(
            externalEventId,
            savedBooking.EventId);

        Assert.Equal(
            externalUserId,
            savedBooking.UserId);
    }

    [Fact]
    public async Task GetActiveBookingsCountByUserIdAsync_Should_Count_Only_Active_Bookings_Of_Selected_User()
    {
        // Arrange
        const long firstUserId = 1;
        const long secondUserId = 2;

        await using var context =
            Fixture.CreateBookingsDbContext();

        var repository =
            new BookingRepository(context);

        var pendingBooking =
            Booking.CreatePending(1, firstUserId);

        var awaitingBooking =
            Booking.CreatePending(2, firstUserId);

        var confirmedBooking =
            Booking.CreatePending(3, firstUserId);

        var rejectedBooking =
            Booking.CreatePending(4, firstUserId);

        var cancelledBooking =
            Booking.CreatePending(5, firstUserId);

        var secondUserBooking =
            Booking.CreatePending(6, secondUserId);

        Assert.True(
            awaitingBooking.TryStartConfirmation());

        Assert.True(
            confirmedBooking.TryStartConfirmation());

        Assert.True(
            confirmedBooking.TryConfirm());

        Assert.True(
            rejectedBooking.TryReject());

        Assert.True(
            cancelledBooking.Cancel());

        await repository.AddAsync(pendingBooking);
        await repository.AddAsync(awaitingBooking);
        await repository.AddAsync(confirmedBooking);
        await repository.AddAsync(rejectedBooking);
        await repository.AddAsync(cancelledBooking);
        await repository.AddAsync(secondUserBooking);

        await repository.SaveChangesAsync();

        // Act
        var firstUserResult =
            await repository
                .GetActiveBookingsCountByUserIdAsync(
                    firstUserId);

        var secondUserResult =
            await repository
                .GetActiveBookingsCountByUserIdAsync(
                    secondUserId);

        // Assert
        Assert.Equal(3, firstUserResult);
        Assert.Equal(1, secondUserResult);
    }

    [Fact]
    public async Task GetAwaitingConfirmationWithoutRequestIdsAsync_Should_Return_Only_Unrequested_Awaiting_Bookings()
    {
        // Arrange
        long expectedBookingId;

        await using (var seedContext =
                     Fixture.CreateBookingsDbContext())
        {
            var repository =
                new BookingRepository(seedContext);

            var awaitingWithoutRequest =
                Booking.CreatePending(1, 1);

            var awaitingWithRequest =
                Booking.CreatePending(2, 1);

            var pendingBooking =
                Booking.CreatePending(3, 1);

            Assert.True(
                awaitingWithoutRequest
                    .TryStartConfirmation());

            Assert.True(
                awaitingWithRequest
                    .TryStartConfirmation());

            Assert.True(
                awaitingWithRequest
                    .MarkConfirmationRequested());

            await repository.AddAsync(
                awaitingWithoutRequest);

            await repository.AddAsync(
                awaitingWithRequest);

            await repository.AddAsync(
                pendingBooking);

            await repository.SaveChangesAsync();

            expectedBookingId =
                awaitingWithoutRequest.Id;
        }

        // Act
        await using var queryContext =
            Fixture.CreateBookingsDbContext();

        var queryRepository =
            new BookingRepository(queryContext);

        var result =
            await queryRepository
                .GetAwaitingConfirmationWithoutRequestIdsAsync();

        // Assert
        Assert.Single(result);

        Assert.Equal(
            expectedBookingId,
            result.Single());
    }
}