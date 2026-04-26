using RestApiEventProject.DTO;

namespace RestApiEventProject.Services;

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
    Task<BookingInfoDto?> CreateBookingAsync(int eventId);

    /// <summary>
    /// Возвращает бронь по её идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns>Информация о брони или null, если бронь не найдена.</returns>
    Task<BookingInfoDto?> GetBookingByIdAsync(long bookingId);
}
