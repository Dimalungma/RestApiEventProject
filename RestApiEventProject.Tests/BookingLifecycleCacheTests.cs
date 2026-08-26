using EventsService.Application;
using EventsService.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RestApiProject.Tests.Infrastructure;

namespace RestApiProject.Tests;

public class BookingLifecycleCacheTests
{
    [Fact]
    public async Task HandleBookingCreatedAsync_Should_Invalidate_Event_Cache_When_Seats_Are_Reserved()
    {
        // Arrange
        const long bookingId = 10;
        const int eventId = 5;
        const int seatsCount = 2;

        var operationLog = new List<string>();

        var eventItem = CreateEvent(
            eventId,
            totalSeats: 10);

        var eventRepository =
            new TrackingEventRepository
            {
                EventById = eventItem
            };

        var reservationRepository =
            new TrackingBookingReservationRepository(
                operationLog);

        var cache =
            new TrackingCacheService(operationLog);

        var publisher =
            new TestEventSeatEventPublisher();

        var handler = CreateHandler(
            eventRepository,
            reservationRepository,
            publisher,
            cache);

        // Act
        await handler.HandleBookingCreatedAsync(
            bookingId,
            eventId,
            seatsCount);

        // Assert
        eventItem.AvailableSeats.Should().Be(8);

        reservationRepository.AddCalls.Should().Be(1);
        reservationRepository.SaveChangesCalls.Should().Be(1);

        reservationRepository.AddedReservation
            .Should()
            .NotBeNull();

        reservationRepository.AddedReservation!.Status
            .Should()
            .Be(BookingReservationStatus.Reserved);

        cache.RemoveCalls.Should().Be(1);
        cache.LastRemovedKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        publisher.ReservedPublishCalls.Should().Be(1);
        publisher.UnavailablePublishCalls.Should().Be(0);

        operationLog.Should().ContainInOrder(
            "ReservationRepository.SaveChanges",
            $"Cache.Remove:{EventCacheKeys.ById(eventId)}");
    }

    [Fact]
    public async Task HandleBookingCreatedAsync_Should_Not_Invalidate_Event_Cache_When_Seats_Are_Not_Reserved()
    {
        // Arrange
        const long bookingId = 10;
        const int eventId = 5;
        const int seatsCount = 2;

        var eventItem = CreateEvent(
            eventId,
            totalSeats: 10);

        eventItem.AvailableSeats = 0;

        var eventRepository =
            new TrackingEventRepository
            {
                EventById = eventItem
            };

        var reservationRepository =
            new TrackingBookingReservationRepository();

        var cache =
            new TrackingCacheService();

        var publisher =
            new TestEventSeatEventPublisher();

        var handler = CreateHandler(
            eventRepository,
            reservationRepository,
            publisher,
            cache);

        // Act
        await handler.HandleBookingCreatedAsync(
            bookingId,
            eventId,
            seatsCount);

        // Assert
        eventItem.AvailableSeats.Should().Be(0);

        cache.RemoveCalls.Should().Be(0);

        reservationRepository.AddedReservation
            .Should()
            .NotBeNull();

        reservationRepository.AddedReservation!.Status
            .Should()
            .Be(BookingReservationStatus.Unavailable);

        publisher.ReservedPublishCalls.Should().Be(0);
        publisher.UnavailablePublishCalls.Should().Be(1);
    }

    [Fact]
    public async Task HandleBookingCancelledAsync_Should_Invalidate_Event_Cache_When_Seats_Are_Released()
    {
        // Arrange
        const long bookingId = 10;
        const int eventId = 5;
        const int seatsCount = 2;

        var operationLog = new List<string>();

        var eventItem = CreateEvent(
            eventId,
            totalSeats: 10);

        eventItem.TryReserveSeats(seatsCount)
            .Should()
            .Be(ReserveSeatsResult.Success);

        eventItem.AvailableSeats.Should().Be(8);

        var reservation =
            BookingReservation.CreateReserved(
                bookingId,
                eventId,
                seatsCount);

        var eventRepository =
            new TrackingEventRepository
            {
                EventById = eventItem
            };

        var reservationRepository =
            new TrackingBookingReservationRepository(
                operationLog)
            {
                ReservationToReturn = reservation
            };

        var cache =
            new TrackingCacheService(operationLog);

        var publisher =
            new TestEventSeatEventPublisher();

        var handler = CreateHandler(
            eventRepository,
            reservationRepository,
            publisher,
            cache);

        // Act
        await handler.HandleBookingCancelledAsync(
            bookingId,
            eventId,
            seatsCount);

        // Assert
        eventItem.AvailableSeats.Should().Be(10);

        reservation.Status.Should()
            .Be(BookingReservationStatus.Cancelled);

        reservationRepository.SaveChangesCalls
            .Should()
            .Be(1);

        cache.RemoveCalls.Should().Be(1);
        cache.LastRemovedKey.Should()
            .Be(EventCacheKeys.ById(eventId));

        operationLog.Should().ContainInOrder(
            "ReservationRepository.SaveChanges",
            $"Cache.Remove:{EventCacheKeys.ById(eventId)}");
    }

    [Fact]
    public async Task HandleBookingCancelledAsync_Should_Not_Invalidate_Event_Cache_When_Reservation_Is_Already_Cancelled()
    {
        // Arrange
        const long bookingId = 10;
        const int eventId = 5;
        const int seatsCount = 2;

        var reservation =
            BookingReservation.CreateCancelled(
                bookingId,
                eventId,
                seatsCount);

        var eventRepository =
            new TrackingEventRepository();

        var reservationRepository =
            new TrackingBookingReservationRepository
            {
                ReservationToReturn = reservation
            };

        var cache =
            new TrackingCacheService();

        var publisher =
            new TestEventSeatEventPublisher();

        var handler = CreateHandler(
            eventRepository,
            reservationRepository,
            publisher,
            cache);

        // Act
        await handler.HandleBookingCancelledAsync(
            bookingId,
            eventId,
            seatsCount);

        // Assert
        cache.RemoveCalls.Should().Be(0);

        eventRepository.GetByIdCalls.Should().Be(0);

        reservationRepository.SaveChangesCalls
            .Should()
            .Be(0);
    }

    private static BookingLifecycleHandler CreateHandler(
        IEventRepository eventRepository,
        IBookingReservationRepository reservationRepository,
        IEventSeatEventPublisher publisher,
        ICacheService cache)
    {
        return new BookingLifecycleHandler(
            eventRepository,
            reservationRepository,
            publisher,
            cache,
            NullLogger<BookingLifecycleHandler>.Instance);
    }

    private static Event CreateEvent(
        int id,
        int totalSeats)
    {
        return new Event(
            "Тестовое событие",
            null,
            FutureUtcDate(1, 10),
            FutureUtcDate(1, 12),
            totalSeats)
        {
            Id = id
        };
    }

    private static DateTime FutureUtcDate(
        int daysFromToday,
        int hour = 0,
        int minute = 0)
    {
        var date =
            DateTime.UtcNow.Date.AddDays(daysFromToday);

        return new DateTime(
            date.Year,
            date.Month,
            date.Day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}