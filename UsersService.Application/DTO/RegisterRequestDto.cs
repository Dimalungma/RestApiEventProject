namespace UsersService.Application;

/// <summary>
/// Запрос на регистрацию пользователя
/// </summary>
public sealed class RegisterRequestDto
{
    /// <summary>
    /// Логин пользователя
    /// </summary>
    public required string Login { get; init; }
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public required string Password { get; init; }

    //Role убран, теперь регистрация админ\пользователь идет через два разных endpoint'а
}