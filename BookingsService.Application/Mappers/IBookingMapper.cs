using BookingsService.Domain;
namespace BookingsService.Application;

public interface IBookingMapper
{
    BookingInfoDto ToResponseDto(Booking booking);
}
