using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.Application;

namespace RestApiEventProject.Presentation.Controllers;

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
    [ProducesResponseType(typeof(BookingInfoDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking(int id, [FromQuery] long userId) //Промежуточно для тестов. TODO заменить на JWT Claims
    {
        var (booking, error) = await _bookingService.CreateBookingAsync(id, userId);

        if (error == BookingCreateError.EventNotFound)
        {
            return NotFound($"Не найдено мероприятие с id {id}");
        }
        if (error == BookingCreateError.EventAlreadyStarted)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Event already started",
                Detail = "Нельзя забронировать уже начавшееся мероприятие."
            });
        }
        if (error == BookingCreateError.NoAvailableSeats) //Никаких exception'ов, это все бизнес логика, а значит нормальные коды ошибок и Result
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No available seats",
                Detail = "На мероприятии нет свободных мест"
            });
        }
        if (error == BookingCreateError.ActiveBookingsLimitExceeded)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Active bookings limit exceeded",
                Detail = "Пользователь не может иметь больше 10 активных бронирований."
            });
        }


        var response = _bookingMapper.ToResponseDto(booking!);

        return Accepted(
            $"/bookings/{booking!.Id}",
            response
        );
    }

    [HttpDelete("bookings/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(
        long id,
        [FromQuery] long userId) //Промежуточно для тестов. TODO заменить на JWT Claims
    {
        var error = await _bookingService.CancelBookingAsync(
            id,
            userId,
            0); //TODO похоже claims вообще надо будет передавать в сервис, потому что presentation не знает о доменной роли

        if (error == BookingCancelError.BookingNotFound)
        {
            return NotFound($"Не найдена бронь с id {id}");
        }

        if (error == BookingCancelError.Forbidden)
        {
            return Forbid();
        }

        return NoContent();
    }

    /// <summary>
    /// Получение брони по id
    /// </summary>
    /// <param name="id">Id брони</param>
    /// <returns></returns>
    [HttpGet("bookings/{id:long}")]
    [ProducesResponseType(typeof(BookingInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(long id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound($"Не найдена бронь с id {id}");

        return Ok(_bookingMapper.ToResponseDto(booking));
    }

#pragma warning disable CS1591 //Тут мб приватные методы
}
