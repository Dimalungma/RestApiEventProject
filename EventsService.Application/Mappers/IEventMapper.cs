using EventsService.Domain;

namespace EventsService.Application
{
    public interface IEventMapper
    {
        Event ToEntity(CreateEventRequestDto dto);
        Event ToEntity(UpdateEventRequestDto dto);
        EventResponseDto ToResponseDto(Event eventItem);
        IEnumerable<EventResponseDto> ToResponseDtoList(IEnumerable<Event> events);
    }
}