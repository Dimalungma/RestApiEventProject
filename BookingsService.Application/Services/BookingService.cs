using BookingsService.Domain;

namespace BookingsService.Application;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingEventPublisher _bookingEventPublisher;

    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingEventPublisher bookingEventPublisher)
    {
        _bookingRepository = bookingRepository;
        _bookingEventPublisher = bookingEventPublisher;
    }


    public async Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId, long userId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var activeBookingsCount = await _bookingRepository.GetActiveBookingsCountByUserIdAsync(userId);
            if (activeBookingsCount >= BookingConstants.MaxActiveBookingsPerUser)
            {
                return (null, BookingCreateError.ActiveBookingsLimitExceeded);
            }

            var booking = Booking.CreatePending(eventId, userId);

            await _bookingRepository.AddAsync(booking);
            await _bookingRepository.SaveChangesAsync();

            return (booking, null);
            //До подтверждения "оплаты" бэкграунд сервисом, в кафку ничего не полетит
        }
        finally
        {
            BookingSemaphore.Release();
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