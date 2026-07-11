namespace RestApiEventProject.Application;

/// <summary>
/// Вход в систему (для получения токена)
/// </summary>
public sealed class LoginUserRequestDto
{
    /// <summary>
    /// Логин
    /// </summary>
    public required string Login { get; init; }
    /// <summary>
    /// Пароль
    /// </summary>
    public required string Password { get; init; }
}