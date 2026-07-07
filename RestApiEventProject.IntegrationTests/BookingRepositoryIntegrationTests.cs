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
        // Arrange
        await using (var context = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(context);
            var bookingRepository = new BookingRepository(context);

            await CreateStoredEventAsync(eventRepository, id: 1);

            var booking = Booking.CreatePending(1, 1);

            // Act
            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();
        }

        // Assert
        await using (var assertContext = Fixture.CreateDbContext())
        {
            var assertRepository = new BookingRepository(assertContext);

            var savedBooking = await assertRepository.GetByIdAsync(1);

            Assert.NotNull(savedBooking);
            Assert.Equal(1, savedBooking.Id);
            Assert.Equal(1, savedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, savedBooking.Status);
            Assert.Null(savedBooking.ProcessedAt);
        }
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
    public async Task GetLastIdAsync_Should_Return_Max_Booking_Id()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            await CreateStoredEventAsync(eventRepository, id: 1);

            await bookingRepository.AddAsync(Booking.CreatePending(1, 1));
            await bookingRepository.AddAsync(Booking.CreatePending(10, 1));
            await bookingRepository.AddAsync(Booking.CreatePending(4, 1));
            await bookingRepository.SaveChangesAsync();
        }

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new BookingRepository(queryContext);

            var result = await repository.GetLastIdAsync();

            // Assert
            Assert.Equal(10, result);
        }
    }

    [Fact]
    public async Task GetLastIdAsync_Should_Return_Zero_When_Bookings_Are_Empty()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var bookingRepository = new BookingRepository(context);

        // Act
        var result = await bookingRepository.GetLastIdAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_Should_Return_Only_Pending_Booking_Ids()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            await CreateStoredEventAsync(eventRepository, id: 1);

            var firstBooking = Booking.CreatePending(1, 1);
            var secondBooking = Booking.CreatePending(2, 1);
            var thirdBooking = Booking.CreatePending(3, 1);

            secondBooking.Confirm();
            thirdBooking.Reject();

            await bookingRepository.AddAsync(firstBooking);
            await bookingRepository.AddAsync(secondBooking);
            await bookingRepository.AddAsync(thirdBooking);
            await bookingRepository.SaveChangesAsync();
        }

        // Act
        await using (var queryContext = Fixture.CreateDbContext())
        {
            var repository = new BookingRepository(queryContext);

            var result = await repository.GetPendingBookingIdsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result.Single());
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Confirmed_Status()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            await CreateStoredEventAsync(eventRepository, id: 1);

            var booking = Booking.CreatePending(1, 1);

            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();
        }

        // Act
        await using (var actContext = Fixture.CreateDbContext())
        {
            var bookingRepository = new BookingRepository(actContext);

            var booking = await bookingRepository.GetByIdAsync(1);

            Assert.NotNull(booking);

            booking.Confirm();

            await bookingRepository.SaveChangesAsync();
        }

        // Assert
        await using (var assertContext = Fixture.CreateDbContext())
        {
            var assertRepository = new BookingRepository(assertContext);

            var savedBooking = await assertRepository.GetByIdAsync(1);

            Assert.NotNull(savedBooking);
            Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);
            Assert.NotNull(savedBooking.ProcessedAt);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Rejected_Status()
    {
        // Arrange
        await using (var seedContext = Fixture.CreateDbContext())
        {
            var eventRepository = new EventRepository(seedContext);
            var bookingRepository = new BookingRepository(seedContext);

            await CreateStoredEventAsync(eventRepository, id: 1);

            var booking = Booking.CreatePending(1, 1);

            await bookingRepository.AddAsync(booking);
            await bookingRepository.SaveChangesAsync();
        }

        // Act
        await using (var actContext = Fixture.CreateDbContext())
        {
            var bookingRepository = new BookingRepository(actContext);

            var booking = await bookingRepository.GetByIdAsync(1);

            Assert.NotNull(booking);

            booking.Reject();

            await bookingRepository.SaveChangesAsync();
        }

        // Assert
        await using (var assertContext = Fixture.CreateDbContext())
        {
            var assertRepository = new BookingRepository(assertContext);

            var savedBooking = await assertRepository.GetByIdAsync(1);

            Assert.NotNull(savedBooking);
            Assert.Equal(BookingStatus.Rejected, savedBooking.Status);
            Assert.NotNull(savedBooking.ProcessedAt);
        }
    }

    [Fact]
    public async Task AddAsync_Should_Respect_Foreign_Key_To_Event()
    {
        // Arrange
        await using var context = Fixture.CreateDbContext();

        var bookingRepository = new BookingRepository(context);

        var booking = Booking.CreatePending(1, 999);

        // Act
        await bookingRepository.AddAsync(booking);

        var exception = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
        {
            await bookingRepository.SaveChangesAsync();
        });

        // Assert
        Assert.NotNull(exception);
    }

    private static async Task CreateStoredEventAsync(EventRepository eventRepository, int id)
    {
        var eventItem = new Event(
            "Тестовое мероприятие",
            "Описание",
            new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            10);

        eventItem.Id = id;

        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();
    }
}