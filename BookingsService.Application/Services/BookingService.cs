using BookingsService.Domain;

namespace BookingsService.Application;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingEventPublisher _bookingEventPublisher;
    private readonly IUserBookingLock _userBookingLock;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingEventPublisher bookingEventPublisher,
            IUserBookingLock userBookingLock)
    {
        _bookingRepository = bookingRepository;
        _bookingEventPublisher = bookingEventPublisher;
        _userBookingLock = userBookingLock;
    }


    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId, long userId)
    {
        using (await _userBookingLock.AcquireAsync(userId))
        {
            var activeBookingsCount =
                await _bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);

            if (activeBookingsCount >= BookingConstants.MaxActiveBookingsPerUser)
            {
                return (null, BookingCreateError.ActiveBookingsLimitExceeded);
            }

            var booking = Booking.CreatePending(eventId, userId);

            await _bookingRepository.AddAsync(booking);
            await _bookingRepository.SaveChangesAsync();

            return (booking, null);
        }
    }

    public async Task<BookingCancelError?> CancelBookingAsync(long bookingId, long userId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking is null)
        {
            return BookingCancelError.BookingNotFound;
        }

        if (!isAdmin && booking.UserId != userId) //Отменить может или сам пользователь, или администратор
        {
            return BookingCancelError.Forbidden;
        }
        var previousStatus = booking.Status;

        if (!booking.Cancel())
        {
            return null;
        }

        await _bookingRepository.SaveChangesAsync();

        if (previousStatus == BookingStatus.AwaitingConfirmation ||
            previousStatus == BookingStatus.Confirmed) //Чтобы если status = pending, не захлямлять кафку. Ну и мб дубли на уже cancelled ивент
        {
            await _bookingEventPublisher.PublishBookingCancelledAsync(
                booking.Id,
                booking.EventId,
                BookingConstants.SeatsPerBooking,
                booking.ProcessedAt!.Value);
        }

        return null;
    }

    public async Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }
}