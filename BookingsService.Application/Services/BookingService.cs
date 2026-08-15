using BookingsService.Domain;

namespace BookingsService.Application;

/// <summary>
/// Сервис для работы с бронированиями мероприятий.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public BookingService(
        IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
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

        if (!booking.Cancel())
        {
            return null;
        }

        await _bookingRepository.SaveChangesAsync();

        //TODO после добавления кафки отправлять BookingCancelled, если уже был статус Confirm

        return null;
    }

    public async Task<Booking?> GetBookingByIdAsync(long bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }
}