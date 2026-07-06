using RestApiEventProject.Domain;
namespace RestApiEventProject.Application;

/// <summary>
/// Описывает сервис для работы с бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создаёт новую бронь для указанного мероприятия.
    /// </summary>
    /// <param name="eventId">Идентификатор мероприятия.</param>
    /// <returns>Информация о созданной брони.</returns>
    Task<(Booking? Booking, BookingCreateError? Error)> CreateBookingAsync(int eventId);

    /// <summary>
    /// Возвращает бронь по её идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns>Информация о брони или null, если бронь не найдена.</returns>
    Task<Booking?> GetBookingByIdAsync(long bookingId);
}
