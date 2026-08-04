namespace UsersService.Application;

/// <summary>
/// Запрос на регистрацию пользователя
/// </summary>
public sealed class RegisterUserRequestDto
{
    /// <summary>
    /// Логин пользователя
    /// </summary>
    public required string Login { get; init; }
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public required string Password { get; init; }
    /// <summary>
    /// Роль, если оставить пустой\null, будет выдано по умолчанию
    /// </summary>
    public string? Role { get; init; }
}