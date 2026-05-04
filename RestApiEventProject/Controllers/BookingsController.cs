using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.Services;

namespace RestApiEventProject.Controllers;

/// <summary>
/// Операции для управления бронированиями мероприятий
/// </summary>
[ApiController]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IBookingMapper _bookingMapper;

#pragma warning disable CS1591 //В этом блоке конструктор

    public BookingsController(IBookingService bookingService, IBookingMapper bookingMapper)
    {
        _bookingService = bookingService;
        _bookingMapper = bookingMapper;
    }

#pragma warning restore CS1591 //Тут методы контроллера для Swagger

    /// <summary>
    /// Создание брони для мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    /// <returns></returns>
    [HttpPost("events/{id:int}/book")]
    public async Task<IActionResult> CreateBooking(int id)
    {
        var booking = await _bookingService.CreateBookingAsync(id);

        if (booking == null)
            return NotFound($"Не найдено мероприятие с id {id}");

        var response = _bookingMapper.ToResponseDto(booking);

        return Accepted(
            $"/bookings/{booking.Id}",
            response
        );
    }

    /// <summary>
    /// Получение брони по id
    /// </summary>
    /// <param name="id">Id брони</param>
    /// <returns></returns>
    [HttpGet("bookings/{id:long}")]
    public async Task<IActionResult> GetBookingById(long id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound($"Не найдена бронь с id {id}");

        return Ok(_bookingMapper.ToResponseDto(booking));
    }

#pragma warning disable CS1591 //Тут мб приватные методы
}
