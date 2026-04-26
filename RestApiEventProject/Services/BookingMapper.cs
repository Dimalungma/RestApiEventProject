using RestApiEventProject.DTO;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services;
public class BookingMapper : IBookingMapper
{
    /// <summary>
    /// Преобразует сущность брони в DTO для ответа.
    /// </summary>
    public BookingInfoDto ToResponseDto(Booking booking)
    {
        return new BookingInfoDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
