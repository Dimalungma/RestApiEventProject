using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.Models;
using RestApiEventProject.Services;

namespace RestApiEventProject.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        // Получить все события через сервис
        return Ok();
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        // Получить событие по id
        // Если не найдено -> return NotFound();
        return Ok();
    }

    [HttpPost]
    public IActionResult Create([FromBody] Event eventItem)
    {
        // Проверить модель
        // Проверить, что EndAt > StartAt
        // Создать событие через сервис
        // Вернуть CreatedAtAction(...)
        return Created();
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] Event eventItem)
    {
        // Проверить модель
        // Проверить даты
        // Попробовать обновить через сервис
        // Если не найдено -> NotFound()
        // Иначе NoContent()
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        // Попробовать удалить через сервис
        // Если не найдено -> NotFound()
        // Иначе NoContent()
        return NoContent();
    }
}
