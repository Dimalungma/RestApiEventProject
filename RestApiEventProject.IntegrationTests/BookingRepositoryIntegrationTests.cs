using Microsoft.EntityFrameworkCore;
using RestApiEventProject.Domain;
using RestApiEventProject.Infrastructure.DataAccess;
using RestApiEventProject.IntegrationTests.Infrastructure;

namespace RestApiEventProject.IntegrationTests;

public class BookingRepositoryIntegrationTests : IntegrationTestBase
{
    public BookingRepositoryIntegrationTests(PostgreSqlTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_Should_Save_Booking_To_PostgreSql()
    {
        long bookingId;
        int eventId;
        long userId;

        // Arrange
        await using (var context = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(context);
            var userRepository = new UserRepository(context);
            var bookingRepository = new BookingRepository(context);

            var eventItem = await CreateStoredEventAsync(eventRepository);
            var user = await CreateStoredUserAsync(userRepository);
            var booking = Booking.CreatePending(eventItem.Id, user.Id);

            // Act
            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            bookingId = booking.Id;
            eventId = eventItem.Id;
            userId = user.Id;
        }

        // Assert
        await using var assertContext = Fixture.CreateDbContext();

        var assertRepository = new BookingRepository(assertContext);
        var savedBooking = await assertRepository.GetByIdAsync(bookingId);

        Assert.True(bookingId > 0);
        Assert.NotNull(savedBooking);
        Assert.Equal(bookingId, savedBooking.Id);
        Assert.Equal(eventId, savedBooking.EventId);
        Assert.Equal(userId, savedBooking.UserId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
        Assert.Null(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Booking_Does_Not_Exist()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var bookingRepository = new BookingRepository(context);

        // Act
        var result = await bookingRepository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_Should_Return_Only_Pending_Booking_Ids()
    {
        long pendingBookingId;

        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var userRepository = new UserRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            var eventItem = await CreateStoredEventAsync(eventRepository);
            var user = await CreateStoredUserAsync(userRepository);

            var firstBooking = Booking.CreatePending(eventItem.Id, user.Id);
            var secondBooking = Booking.CreatePending(eventItem.Id, user.Id);
            var thirdBooking = Booking.CreatePending(eventItem.Id, user.Id);

            secondBooking.Confirm();
            thirdBooking.Reject();

            await bookingRepository.AddAsync(firstBooking);
            await bookingRepository.AddAsync(secondBooking);
            await bookingRepository.AddAsync(thirdBooking);
            await bookingRepository.SaveChangesAsync();

            pendingBookingId = firstBooking.Id;
        }

        // Act
        await using var queryContext = Fixture.CreateDbContext();

        var repository = new BookingRepository(queryContext);
        var result = await repository.GetPendingBookingIdsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(pendingBookingId, result.Single());
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Confirmed_Status()
    {
        long bookingId;

        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var userRepository = new UserRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            var eventItem = await CreateStoredEventAsync(eventRepository);
            var user = await CreateStoredUserAsync(userRepository);
            var booking = Booking.CreatePending(eventItem.Id, user.Id);

            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            bookingId = booking.Id;
        }

        // Act
        await using (var actContext = Fixture.CreateDbContext())
        {
            var bookingRepository = new BookingRepository(actContext);
            var booking = await bookingRepository.GetByIdAsync(bookingId);

            Assert.NotNull(booking);

            booking.Confirm();
            await bookingRepository.SaveChangesAsync();
        }

        // Assert
        await using var assertContext = Fixture.CreateDbContext();

        var assertRepository = new BookingRepository(assertContext);
        var savedBooking = await assertRepository.GetByIdAsync(bookingId);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);
        Assert.NotNull(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Rejected_Status()
    {
        long bookingId;

        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var userRepository = new UserRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            var eventItem = await CreateStoredEventAsync(eventRepository);
            var user = await CreateStoredUserAsync(userRepository);
            var booking = Booking.CreatePending(eventItem.Id, user.Id);

            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();

            bookingId = booking.Id;
        }

        // Act
        await using (var actContext = Fixture.CreateDbContext())
        {
            var bookingRepository = new BookingRepository(actContext);
            var booking = await bookingRepository.GetByIdAsync(bookingId);

            Assert.NotNull(booking);

            booking.Reject();
            await bookingRepository.SaveChangesAsync();
        }

        // Assert
        await using var assertContext = Fixture.CreateDbContext();

        var assertRepository = new BookingRepository(assertContext);
        var savedBooking = await assertRepository.GetByIdAsync(bookingId);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Rejected, savedBooking.Status);
        Assert.NotNull(savedBooking.ProcessedAt);
    }

    [Fact]
    public async Task AddAsync_Should_Respect_Foreign_Key_To_Event()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var userRepository = new UserRepository(context);
        var bookingRepository = new BookingRepository(context);
        var user = await CreateStoredUserAsync(userRepository);

        var booking = Booking.CreatePending(
            eventId: 999,
            userId: user.Id);

        // Act
        await bookingRepository.AddAsync(booking);

        var exception = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            async () =>
            {
                await bookingRepository.SaveChangesAsync();
            });

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task AddAsync_Should_Respect_Foreign_Key_To_User()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = await CreateStoredEventAsync(eventRepository);
        var booking = Booking.CreatePending(eventItem.Id, userId: 999);

        // Act
        await bookingRepository.AddAsync(booking);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () =>
            {
                await bookingRepository.SaveChangesAsync();
            });

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GetActiveBookingsCountByUserIdAsync_Should_Count_Only_Active_Bookings_Of_Selected_User()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var eventRepository = new EventRepository(context);
        var userRepository = new UserRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = await CreateStoredEventAsync(eventRepository);
        var firstUser = await CreateStoredUserAsync(userRepository, "first-user");
        var secondUser = await CreateStoredUserAsync(userRepository, "second-user");

        var pendingBooking = Booking.CreatePending(eventItem.Id, firstUser.Id);
        var confirmedBooking = Booking.CreatePending(eventItem.Id, firstUser.Id);
        var rejectedBooking = Booking.CreatePending(eventItem.Id, firstUser.Id);
        var cancelledBooking = Booking.CreatePending(eventItem.Id, firstUser.Id);
        var secondUserBooking = Booking.CreatePending(eventItem.Id, secondUser.Id);

        confirmedBooking.Confirm();
        rejectedBooking.Reject();
        cancelledBooking.Cancel();

        await bookingRepository.AddAsync(pendingBooking);
        await bookingRepository.AddAsync(confirmedBooking);
        await bookingRepository.AddAsync(rejectedBooking);
        await bookingRepository.AddAsync(cancelledBooking);
        await bookingRepository.AddAsync(secondUserBooking);
        await bookingRepository.SaveChangesAsync();

        // Act
        var firstUserResult = await bookingRepository.GetActiveBookingsCountByUserIdAsync(firstUser.Id);
        var secondUserResult = await bookingRepository.GetActiveBookingsCountByUserIdAsync(secondUser.Id);

        // Assert
        Assert.Equal(2, firstUserResult);
        Assert.Equal(1, secondUserResult);
    }

    private static async Task<Event> CreateStoredEventAsync(EventRepository eventRepository)
    {
        var eventItem = new Event(
            "Тестовое мероприятие",
            "Описание",
            new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            10);

        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        return eventItem;
    }

    private static async Task<User> CreateStoredUserAsync(
        UserRepository userRepository,
        string login = "integration-user")
    {
        var user = User.Create(
            login,
            "TEST_PASSWORD_HASH",
            UserRole.User);

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        return user;
    }
}