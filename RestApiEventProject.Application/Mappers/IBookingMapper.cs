using RestApiEventProject.Domain;
namespace RestApiEventProject.Application;

public interface IBookingMapper
{
    BookingInfoDto ToResponseDto(Booking booking);
}
