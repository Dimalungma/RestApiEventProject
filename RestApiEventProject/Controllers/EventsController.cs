using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.Application;
using Microsoft.AspNetCore.Authorization;

namespace RestApiEventProject.Presentation.Controllers;

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
    [ProducesResponseType(typeof(PaginatedResult<EventResponseDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetEventsQuery query)
    {
        var pagedEvents = await _eventService.GetAllAsync(query);
        var response = new PaginatedResult<EventResponseDto> //Придется дублировать, иначе не вижу как удержать DTO
        {
            TotalCount = pagedEvents.TotalCount,
            Page = pagedEvents.Page,
            CurrentItemCount = pagedEvents.CurrentItemCount,
            Items = _eventMapper.ToResponseDtoList(pagedEvents.Items)
        };

        return Ok(response);
    }
    /// <summary>
    /// Запрос конкретного мероприятия
    /// </summary>
    /// <param name="id">Id запрашиваемого мероприятия</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var foundevent = await _eventService.GetByIdAsync(id);

        if (foundevent == null)
            return NotFound($"Не найдено мероприятие с id {id}");

        return Ok(_eventMapper.ToResponseDto(foundevent));
    }
    /// <summary>
    /// Публикация нового мероприятия
    /// </summary>
    /// <param name="eventItem">Описание мероприятия</param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequestDto eventItem)
    {
        var newevent = _eventMapper.ToEntity(eventItem);
        var createdevent = await _eventService.CreateAsync(newevent);

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
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequestDto eventItem)
    {
        var updateevent = _eventMapper.ToEntity(eventItem);
        var result = await _eventService.UpdateAsync(id, updateevent);
        return result switch
        {
            EventUpdateResult.Success => NoContent(),
            EventUpdateResult.NotFound => NotFound(),
            EventUpdateResult.InvalidTotalSeats => BadRequest("Некорректное число мест"),
            EventUpdateResult.TotalSeatsLessThanReservedSeats => Conflict("Нельзя уменьшить TotalSeats ниже количества уже занятых мест"),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    /// <summary>
    /// Удалить существующее меропрятие
    /// </summary>
    /// <param name="id">номер мероприятия</param>
    /// <returns></returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _eventService.DeleteAsync(id))
            return NoContent();
        else
            return NotFound();
    }

#pragma warning disable CS1591 //Тут мб какие нибудь приватные методы, хотя не уверен что все не будет в сервисе
}
