using RestApiEventProject.DTO;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services
{
    public interface IEventMapper
    {
        Event ToEntity(CreateEventRequestDto dto);
        Event ToEntity(UpdateEventRequestDto dto);
        EventResponseDto ToResponseDto(Event eventItem);
        IEnumerable<EventResponseDto> ToResponseDtoList(IEnumerable<Event> events);
    }
}