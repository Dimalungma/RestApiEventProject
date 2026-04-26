using RestApiEventProject.DTO;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public interface IBookingMapper
{
    BookingInfoDto MapToDto(Booking booking);
}
