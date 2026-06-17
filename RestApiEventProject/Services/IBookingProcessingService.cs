using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public interface IBookingProcessingService
{
    //Вынес в отдельный интерфейс, чтобы потом можно было красиво разделить при переходе на EF, а сейчас не снимать private с _bookings

    [Obsolete]
    Task<IReadOnlyCollection<Booking>> GetPendingBookingsAsync();

    [Obsolete]
    Task<bool> ConfirmBookingAsync(long bookingId);

    [Obsolete]
    Task<bool> RejectBookingAsync(long bookingId);
}
