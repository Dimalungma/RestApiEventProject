using RestApiEventProject.DTO;
using RestApiEventProject.Models;

namespace RestApiEventProject.Services;

public class EventMapper : IEventMapper
{
    public Event ToEntity(CreateEventRequestDto dto)
    {
        return new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats
        };
    }

    public Event ToEntity(UpdateEventRequestDto dto)
    {
        return new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.AvailableSeats ?? dto.TotalSeats
            
        };
    }

    public EventResponseDto ToResponseDto(Event eventItem)
    {
        return new EventResponseDto
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartAt = eventItem.StartAt,
            EndAt = eventItem.EndAt
        };
    }

    public IEnumerable<EventResponseDto> ToResponseDtoList(IEnumerable<Event> events)
    {
        return events.Select(ToResponseDto);
    }
}
