using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.DTO;
using RestApiEventProject.Models;
using RestApiEventProject.Services;

namespace RestApiEventProject.Controllers;

/// <summary>
/// CRUD операции для управления мероприятиями
/// </summary>
[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventMapper _eventMapper;
    
    public EventsController(IEventService eventService, IEventMapper eventMapper)
    {
        _eventService = eventService;
        _eventMapper = eventMapper;
    }
    /// <summary>
    /// Запрос всех мероприятий (возможна фильтрация)
    /// </summary>
    /// <param name="query">фильтры запроса</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetAll([FromQuery] GetEventsQuery query)
    {
        var events = _eventService.GetAll(query);
        return Ok(_eventMapper.ToResponseDtoList(events));
    }
    /// <summary>
    /// Запрос конкретного мероприятия
    /// </summary>
    /// <param name="id">Id запрашиваемого мероприятия</param>
    /// <returns></returns>
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var foundevent = _eventService.GetById(id);
        if(foundevent == null)
            return NotFound($"Не найдено мероприятие с id {id}");
        return Ok(_eventMapper.ToResponseDto(foundevent));
    }
    /// <summary>
    /// Публикация нового мероприятия
    /// </summary>
    /// <param name="eventItem">Описание мероприятия</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Create([FromBody] CreateEventRequestDto eventItem)
    {
        var newevent = _eventMapper.ToEntity(eventItem);
        var createdevent = _eventService.Create(newevent);
        return CreatedAtAction(
            nameof(GetById),
            new { id = createdevent.Id },
            _eventMapper.ToResponseDto(createdevent)
            );
    }
    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    /// <param name="id">номер мероприятия</param>
    /// <param name="eventItem">новые поля</param>
    /// <returns></returns>
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpdateEventRequestDto eventItem)
    {
        var updateevent = _eventMapper.ToEntity(eventItem);
        if (_eventService.Update(id, updateevent))
            return NoContent();
        else
            return NotFound();
    }
    /// <summary>
    /// Удалить существующее меропрятие
    /// </summary>
    /// <param name="id">номер мероприятия</param>
    /// <returns></returns>
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if(_eventService.Delete(id))
            return NoContent();
        else
            return NotFound();
    }
}
