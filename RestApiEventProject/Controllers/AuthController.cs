using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestApiEventProject.Application;

namespace RestApiEventProject.Presentation.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryParseRole(request.Role, out var isAdmin))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid role",
                Detail = "Допустимые роли: User и Admin."
            });
        }

        var error = await _userService.RegisterAsync(
            request.Login,
            request.Password,
            isAdmin,
            cancellationToken);

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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Invalid credentials",
                Detail = "Неверный логин или пароль."
            });
        }

        return Ok(new LoginResponseDto
        {
            Token = token!
        });
    }

    private static bool TryParseRole(
        string? role,
        out bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(role) ||
            string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
        {
            isAdmin = false;
            return true;
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            isAdmin = true;
            return true;
        }

        isAdmin = false;
        return false;
    }
}