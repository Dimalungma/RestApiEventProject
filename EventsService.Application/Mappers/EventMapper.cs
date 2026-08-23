using EventsService.Domain;

namespace EventsService.Application;

public class EventMapper : IEventMapper
{
    public Event ToEntity(CreateEventRequestDto dto)
    {
        return new Event(
            dto.Title,
            dto.Description,
            dto.StartAt.ToUtcNormalized(),
            dto.EndAt.ToUtcNormalized(),
            dto.TotalSeats);
    }

    public Event ToEntity(UpdateEventRequestDto dto)
    {
        return new Event(
            dto.Title,
            dto.Description,
            dto.StartAt.ToUtcNormalized(),
            dto.EndAt.ToUtcNormalized(),
            dto.TotalSeats);
    }

    public EventResponseDto ToResponseDto(Event eventItem)
    {
        return new EventResponseDto
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartAt = eventItem.StartAt,
            EndAt = eventItem.EndAt,
            TotalSeats = eventItem.TotalSeats,
            AvailableSeats = eventItem.AvailableSeats
        };
    }

    public IEnumerable<EventResponseDto> ToResponseDtoList(IEnumerable<Event> events)
    {
        return events.Select(ToResponseDto);
    }
}
