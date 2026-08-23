using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersService.Application;

namespace UsersService.Presentation.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {

        var error = await _userService.RegisterAsync(
            request.Login,
            request.Password,
            false,
            cancellationToken);

        return MapRegisterResult(error);
    }


    [HttpPost("register-admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterAdmin(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var error = await _userService.RegisterAsync(
            request.Login,
            request.Password,
            true,
            cancellationToken);

        return MapRegisterResult(error);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var (token, error) = await _userService.LoginAsync(
            request.Login,
            request.Password,
            cancellationToken);

        if (error == UserLoginError.InvalidCredentials)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "Неверный логин или пароль."
            });
        }

        return Ok(new LoginResponseDto
        {
            Token = token!
        });
    }

    private IActionResult MapRegisterResult(UserRegisterError? error)
    {
        return error switch
        {
            null => NoContent(),

            UserRegisterError.InvalidLogin => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid login",
                Detail = "Логин не может быть пустым."
            }),

            UserRegisterError.InvalidPassword => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid password",
                Detail = "Пароль не может быть пустым."
            }),

            UserRegisterError.PasswordTooSimple => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Password too simple",
                Detail = "Пароль не может быть слишком коротким, состоять из одного и того же символа, или 1234..."
            }),

            UserRegisterError.LoginAlreadyExists => BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Login already exists",
                Detail = "Пользователь с таким логином уже существует."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}