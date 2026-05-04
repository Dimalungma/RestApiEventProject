using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.DTO;
using RestApiEventProject.Models;
using RestApiEventProject.Queries;
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
    #pragma warning disable CS1591 //В этом блоке буду писать все конструкторы и свойства, чтобы без предупреждения
    public EventsController(IEventService eventService, IEventMapper eventMapper)
    {
        _eventService = eventService;
        _eventMapper = eventMapper;
    }
    #pragma warning restore CS1591 //Тут все контроллеры, чтобы генерилась документация
    /// <summary>
    /// Запрос всех мероприятий (возможна фильтрация)
    /// </summary>
    /// <param name="query">фильтры запроса</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetAll([FromQuery] GetEventsQuery query)
    {
        var paged_events = _eventService.GetAll(query);
        var response = new PaginatedResult<EventResponseDto> //Придется дублировать, иначе не вижу как удержать DTO
        {
            TotalCount = paged_events.TotalCount,
            Page = paged_events.Page,
            CurrentItemCount = paged_events.CurrentItemCount,
            Items = _eventMapper.ToResponseDtoList(paged_events.Items)
        };
        return Ok(response);
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

    #pragma warning disable CS1591 //Тут мб какие нибудь приватные методы, хотя не уверен что все не будет в сервисе
}
